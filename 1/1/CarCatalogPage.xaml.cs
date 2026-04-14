using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Npgsql;

namespace Прокат_авто
{
	public partial class CarCatalogPage : Page
	{
		private int clientId;
		private string connectionString;
		private ObservableCollection<CarItemViewModel> allCars;
		private ObservableCollection<CarItemViewModel> filteredCars;
		private string imagesPath;

		public CarCatalogPage(int clientId, string connectionString)
		{
			InitializeComponent();
			this.clientId = clientId;
			this.connectionString = connectionString;

			// Путь к папке с фото
			imagesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Cars");

			// Создаем папку если её нет
			if (!Directory.Exists(imagesPath))
			{
				try
				{
					Directory.CreateDirectory(imagesPath);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Не удалось создать папку Images: {ex.Message}");
				}
			}

			// Инициализация коллекций
			allCars = new ObservableCollection<CarItemViewModel>();
			filteredCars = new ObservableCollection<CarItemViewModel>();

			LoadCars();
		}

		private void LoadCars()
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT c.id_car, c.stamp, c.model, c.year_of_release, c.colour, c.mileage, 
                               COALESCE(s.title, 'Неизвестно') as status_title, 
                               COALESCE(r.title, 'Стандарт') as rate_title, 
                               COALESCE(r.price, 2000) as price
                        FROM public.car c
                        LEFT JOIN public.status s ON c.id_status = s.id_status
                        LEFT JOIN public.rate r ON c.id_rate = r.id_rate
                        ORDER BY c.id_car";

					using (var cmd = new NpgsqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						allCars.Clear();
						while (reader.Read())
						{
							string stamp = reader.IsDBNull(1) ? "Не указано" : reader.GetString(1);
							string model = reader.IsDBNull(2) ? "Не указано" : reader.GetString(2);

							var car = new CarItemViewModel
							{
								IdCar = reader.GetInt32(0),
								Stamp = stamp,
								Model = model,
								YearOfRelease = reader.GetInt16(3),
								Colour = reader.IsDBNull(4) ? "Не указан" : reader.GetString(4),
								Mileage = reader.GetDecimal(5),
								StatusTitle = reader.GetString(6),
								RateTitle = reader.GetString(7),
								PricePerDay = reader.GetDecimal(8),
								PhotoPath = GetPhotoPath(stamp, model)
							};
							allCars.Add(car);
						}
					}
				}

				ApplyFiltersAndSort();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки автомобилей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				allCars = new ObservableCollection<CarItemViewModel>();
				filteredCars = new ObservableCollection<CarItemViewModel>();
				carsItemsControl.ItemsSource = filteredCars;
			}
		}

		private string GetPhotoPath(string stamp, string model)
		{
			// Формируем имя файла из марки и модели
			string fileName = $"{stamp}_{model}".ToLower()
				.Replace(" ", "_")
				.Replace("-", "_")
				.Replace("'", "")
				.Replace("\"", "");

			// Проверяем различные расширения
			string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

			foreach (var ext in extensions)
			{
				string fullPath = System.IO.Path.Combine(imagesPath, fileName + ext);
				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}

			// Пробуем вариант только с маркой
			string onlyStamp = stamp.ToLower().Replace(" ", "_").Replace("-", "_");
			foreach (var ext in extensions)
			{
				string fullPath = System.IO.Path.Combine(imagesPath, onlyStamp + ext);
				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}

			// Пробуем вариант только с моделью
			string onlyModel = model.ToLower().Replace(" ", "_").Replace("-", "_");
			foreach (var ext in extensions)
			{
				string fullPath = System.IO.Path.Combine(imagesPath, onlyModel + ext);
				if (File.Exists(fullPath))
				{
					return fullPath;
				}
			}

			// Если фото не найдено, возвращаем фото по умолчанию
			string defaultPath = System.IO.Path.Combine(imagesPath, "default_car.jpg");
			if (File.Exists(defaultPath))
			{
				return defaultPath;
			}

			// Если и дефолтного нет, возвращаем null
			return null;
		}

		private void ApplyFiltersAndSort()
		{
			try
			{
				// Проверка carsItemsControl
				if (carsItemsControl == null)
				{
					System.Diagnostics.Debug.WriteLine("carsItemsControl == null, не могу установить ItemsSource");
					return;
				}

				// Проверка, что allCars не null
				if (allCars == null)
				{
					allCars = new ObservableCollection<CarItemViewModel>();
				}

				// Создаем список из allCars
				var query = new List<CarItemViewModel>();
				foreach (var car in allCars)
				{
					query.Add(car);
				}

				// Фильтр по поиску
				if (txtSearch != null)
				{
					string searchText = txtSearch.Text?.ToLower() ?? "";
					if (!string.IsNullOrEmpty(searchText))
					{
						query = query.FindAll(c => (c.Stamp?.ToLower() ?? "").Contains(searchText) ||
												   (c.Model?.ToLower() ?? "").Contains(searchText));
					}
				}

				// Фильтр по статусу
				if (cmbStatusFilter != null && cmbStatusFilter.SelectedItem is ComboBoxItem selectedItem)
				{
					var selectedStatus = selectedItem.Content?.ToString();
					if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "Все")
					{
						string filterStatus = selectedStatus.ToLower();
						query = query.FindAll(c =>
						{
							string carStatus = c.StatusTitle?.ToLower() ?? "";
							return carStatus == filterStatus;
						});
					}
				}

				// Сортировка
				if (cmbSort != null && cmbSort.SelectedItem is ComboBoxItem sortItem)
				{
					var sortOption = sortItem.Content?.ToString();
					switch (sortOption)
					{
						case "По цене (возрастание)":
							query.Sort((a, b) => a.PricePerDay.CompareTo(b.PricePerDay));
							break;
						case "По цене (убывание)":
							query.Sort((a, b) => b.PricePerDay.CompareTo(a.PricePerDay));
							break;
						case "По марке (А-Я)":
							query.Sort((a, b) => string.Compare(a.Stamp, b.Stamp, StringComparison.Ordinal));
							break;
						case "По марке (Я-А)":
							query.Sort((a, b) => string.Compare(b.Stamp, a.Stamp, StringComparison.Ordinal));
							break;
						case "По году выпуска (новые)":
							query.Sort((a, b) => b.YearOfRelease.CompareTo(a.YearOfRelease));
							break;
						case "По году выпуска (старые)":
							query.Sort((a, b) => a.YearOfRelease.CompareTo(b.YearOfRelease));
							break;
					}
				}

				// Обновляем filteredCars
				if (filteredCars == null)
				{
					filteredCars = new ObservableCollection<CarItemViewModel>();
				}

				filteredCars.Clear();
				foreach (var car in query)
				{
					filteredCars.Add(car);
				}

				carsItemsControl.ItemsSource = filteredCars;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка ApplyFiltersAndSort: {ex.Message}");
			}
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFiltersAndSort();
		}

		private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ApplyFiltersAndSort();
		}

		private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ApplyFiltersAndSort();
		}

		private void RentCar_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (sender is Button button && button.Tag is int carId)
				{
					var rentWindow = new RentCarWindow(clientId, carId, connectionString);
					rentWindow.Owner = Window.GetWindow(this);
					rentWindow.ShowDialog();
					LoadCars(); // Обновляем список после аренды
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка RentCar_Click: {ex.Message}");
				MessageBox.Show($"Ошибка при аренде: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	public class CarItemViewModel : INotifyPropertyChanged
	{
		private int _idCar;
		private string _stamp;
		private string _model;
		private short _yearOfRelease;
		private string _colour;
		private decimal _mileage;
		private string _statusTitle;
		private string _rateTitle;
		private decimal _pricePerDay;
		private string _photoPath;
		private ImageSource _carImage;

		public int IdCar
		{
			get => _idCar;
			set { _idCar = value; OnPropertyChanged(nameof(IdCar)); }
		}

		public string Stamp
		{
			get => _stamp ?? "";
			set
			{
				_stamp = value;
				OnPropertyChanged(nameof(Stamp));
				OnPropertyChanged(nameof(PhotoPath));
				OnPropertyChanged(nameof(CarImage));
			}
		}

		public string Model
		{
			get => _model ?? "";
			set
			{
				_model = value;
				OnPropertyChanged(nameof(Model));
				OnPropertyChanged(nameof(PhotoPath));
				OnPropertyChanged(nameof(CarImage));
			}
		}

		public short YearOfRelease
		{
			get => _yearOfRelease;
			set { _yearOfRelease = value; OnPropertyChanged(nameof(YearOfRelease)); }
		}

		public string Colour
		{
			get => _colour ?? "";
			set { _colour = value; OnPropertyChanged(nameof(Colour)); }
		}

		public decimal Mileage
		{
			get => _mileage;
			set { _mileage = value; OnPropertyChanged(nameof(Mileage)); }
		}

		public string StatusTitle
		{
			get => _statusTitle ?? "";
			set
			{
				_statusTitle = value;
				OnPropertyChanged(nameof(StatusTitle));
				OnPropertyChanged(nameof(CanRent));
				OnPropertyChanged(nameof(StatusColor));
			}
		}

		public string RateTitle
		{
			get => _rateTitle ?? "";
			set { _rateTitle = value; OnPropertyChanged(nameof(RateTitle)); }
		}

		public decimal PricePerDay
		{
			get => _pricePerDay;
			set { _pricePerDay = value; OnPropertyChanged(nameof(PricePerDay)); }
		}

		public string PhotoPath
		{
			get => _photoPath;
			set
			{
				_photoPath = value;
				OnPropertyChanged(nameof(PhotoPath));
				OnPropertyChanged(nameof(CarImage));
			}
		}

		public ImageSource CarImage
		{
			get
			{
				if (string.IsNullOrEmpty(PhotoPath) || !File.Exists(PhotoPath))
					return null;

				try
				{
					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.UriSource = new Uri(PhotoPath, UriKind.Absolute);
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.EndInit();
					return bitmap;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения {PhotoPath}: {ex.Message}");
					return null;
				}
			}
		}

		public bool CanRent => StatusTitle == "свободен" || StatusTitle == "Свободен";

		public Brush StatusColor
		{
			get
			{
				string status = StatusTitle?.ToLower() ?? "";
				switch (status)
				{
					case "свободен":
						return new SolidColorBrush(Colors.Green);
					case "в аренде":
						return new SolidColorBrush(Colors.Orange);
					case "на то":
						return new SolidColorBrush(Colors.Red);
					case "продан":
						return new SolidColorBrush(Colors.Gray);
					default:
						return new SolidColorBrush(Colors.Black);
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}