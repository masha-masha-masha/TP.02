using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Прокат_авто
{
	public partial class ClientContractsPage : Page
	{
		private int clientId;
		private string connectionString;
		private ObservableCollection<ContractItemViewModel> allContracts;
		private ObservableCollection<ContractItemViewModel> filteredContracts;

		public ClientContractsPage(int clientId, string connectionString)
		{
			InitializeComponent();
			this.clientId = clientId;
			this.connectionString = connectionString;

			allContracts = new ObservableCollection<ContractItemViewModel>();
			filteredContracts = new ObservableCollection<ContractItemViewModel>();

			LoadContracts();
		}

		private void LoadContracts()
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT 
                            c.id_contract, 
                            c.""rental amount"", 
                            c.""start date"", 
                            c.""end date"", 
                            c.""total amount"", 
                            COALESCE(s.title, 'Неизвестно') as status_title,
                            car.stamp, 
                            car.model
                        FROM public.contract c
                        LEFT JOIN public.starus_of_the_agreement s ON c.id_starus_of_the_agreement = s.id_starus_of_the_agreement
                        LEFT JOIN public.car car ON c.id_car = car.id_car
                        WHERE c.id_client = @clientId
                        ORDER BY c.id_contract DESC";

					using (var cmd = new NpgsqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@clientId", clientId);
						using (var reader = cmd.ExecuteReader())
						{
							allContracts.Clear();
							while (reader.Read())
							{
								DateTime startDate = reader.GetDateTime(2);
								DateTime endDate = reader.GetDateTime(3);
								int days = (endDate - startDate).Days;

								var contract = new ContractItemViewModel
								{
									ContractId = reader.GetInt32(0),
									RentalAmount = reader.GetDecimal(1),
									StartDate = startDate.ToShortDateString(),
									EndDate = endDate.ToShortDateString(),
									TotalAmount = reader.GetDecimal(4),
									Status = reader.GetString(5),
									CarName = $"{reader.GetString(6)} {reader.GetString(7)}",
									DaysCount = days
								};
								allContracts.Add(contract);
							}
						}
					}
				}

				ApplyFilters();
				UpdateStatistics();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка загрузки договоров: {ex.Message}");
				MessageBox.Show($"Ошибка загрузки договоров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ApplyFilters()
		{
			try
			{
				if (allContracts == null) return;

				var query = new ObservableCollection<ContractItemViewModel>();
				foreach (var contract in allContracts)
				{
					query.Add(contract);
				}

				// Фильтр по статусу
				if (cmbStatusFilter?.SelectedItem is ComboBoxItem selectedStatus)
				{
					string status = selectedStatus.Content.ToString();
					if (status != "Все договоры")
					{
						var tempList = new ObservableCollection<ContractItemViewModel>();
						foreach (var contract in query)
						{
							if ((status == "Активные" && contract.Status == "активен") ||
								(status == "Завершенные" && contract.Status == "завершен") ||
								(status == "Отмененные" && contract.Status == "отменен"))
							{
								tempList.Add(contract);
							}
						}
						query = tempList;
					}
				}

				// Поиск по автомобилю
				string searchText = txtSearch?.Text?.ToLower() ?? "";
				if (!string.IsNullOrEmpty(searchText))
				{
					var tempList = new ObservableCollection<ContractItemViewModel>();
					foreach (var contract in query)
					{
						if (contract.CarName.ToLower().Contains(searchText))
						{
							tempList.Add(contract);
						}
					}
					query = tempList;
				}

				filteredContracts.Clear();
				foreach (var contract in query)
				{
					filteredContracts.Add(contract);
				}

				if (contractsItemsControl != null)
					contractsItemsControl.ItemsSource = filteredContracts;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка ApplyFilters: {ex.Message}");
			}
		}

		private void UpdateStatistics()
		{
			try
			{
				if (allContracts == null) return;

				int total = allContracts.Count;
				int active = 0;
				int completed = 0;

				foreach (var contract in allContracts)
				{
					if (contract.Status == "активен") active++;
					if (contract.Status == "завершен") completed++;
				}

				txtTotalContracts.Text = total.ToString();
				txtActiveContracts.Text = active.ToString();
				txtCompletedContracts.Text = completed.ToString();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка UpdateStatistics: {ex.Message}");
			}
		}

		private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ApplyFilters();
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		private void ViewDetails_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button?.Tag != null)
			{
				int contractId = (int)button.Tag;
				var detailsWindow = new ContractDetailsWindow(contractId, connectionString);
				detailsWindow.Owner = Window.GetWindow(this);
				detailsWindow.ShowDialog();
			}
		}

		private void CancelContract_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button?.Tag != null)
			{
				int contractId = (int)button.Tag;

				var result = MessageBox.Show($"Вы уверены, что хотите отменить договор №{contractId}?",
											"Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

				if (result == MessageBoxResult.Yes)
				{
					try
					{
						using (var conn = new NpgsqlConnection(connectionString))
						{
							conn.Open();

							using (var transaction = conn.BeginTransaction())
							{
								// Получаем ID статуса "отменен"
								string getCancelStatusSql = "SELECT id_starus_of_the_agreement FROM public.starus_of_the_agreement WHERE title = 'отменен'";
								int cancelStatusId = 0;
								using (var cmd = new NpgsqlCommand(getCancelStatusSql, conn, transaction))
								{
									var statusResult = cmd.ExecuteScalar();
									if (statusResult != null && statusResult != DBNull.Value)
									{
										cancelStatusId = Convert.ToInt32(statusResult);
									}
									else
									{
										transaction.Rollback();
										MessageBox.Show("В системе отсутствует статус 'отменен'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
										return;
									}
								}

								// Получаем ID статуса "свободен" для автомобиля
								string getFreeStatusSql = "SELECT id_status FROM public.status WHERE title = 'свободен'";
								int freeStatusId = 0;
								using (var cmd = new NpgsqlCommand(getFreeStatusSql, conn, transaction))
								{
									var statusResult = cmd.ExecuteScalar();
									if (statusResult != null && statusResult != DBNull.Value)
									{
										freeStatusId = Convert.ToInt32(statusResult);
									}
									else
									{
										transaction.Rollback();
										MessageBox.Show("В системе отсутствует статус 'свободен'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
										return;
									}
								}

								// Обновляем статус договора
								string updateContractSql = "UPDATE public.contract SET id_starus_of_the_agreement = @statusId WHERE id_contract = @contractId";
								using (var cmd = new NpgsqlCommand(updateContractSql, conn, transaction))
								{
									cmd.Parameters.AddWithValue("@statusId", cancelStatusId);
									cmd.Parameters.AddWithValue("@contractId", contractId);
									cmd.ExecuteNonQuery();
								}

								// Получаем id_car из договора
								string getCarIdSql = "SELECT id_car FROM public.contract WHERE id_contract = @contractId";
								int carId = 0;
								using (var cmd = new NpgsqlCommand(getCarIdSql, conn, transaction))
								{
									var carIdResult = cmd.ExecuteScalar();
									if (carIdResult != null && carIdResult != DBNull.Value)
									{
										carId = Convert.ToInt32(carIdResult);
									}
								}

								// Обновляем статус автомобиля
								if (carId > 0)
								{
									string updateCarSql = "UPDATE public.car SET id_status = @freeStatusId WHERE id_car = @carId";
									using (var cmd = new NpgsqlCommand(updateCarSql, conn, transaction))
									{
										cmd.Parameters.AddWithValue("@freeStatusId", freeStatusId);
										cmd.Parameters.AddWithValue("@carId", carId);
										cmd.ExecuteNonQuery();
									}
								}

								transaction.Commit();
							}
						}

						MessageBox.Show($"Договор №{contractId} успешно отменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						LoadContracts();
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Ошибка при отмене договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
		}
	}

	public class ContractItemViewModel : INotifyPropertyChanged
	{
		private int _contractId;
		private decimal _rentalAmount;
		private string _startDate;
		private string _endDate;
		private decimal _totalAmount;
		private string _status;
		private string _carName;
		private int _daysCount;

		public int ContractId
		{
			get => _contractId;
			set { _contractId = value; OnPropertyChanged(nameof(ContractId)); }
		}

		public decimal RentalAmount
		{
			get => _rentalAmount;
			set { _rentalAmount = value; OnPropertyChanged(nameof(RentalAmount)); }
		}

		public string StartDate
		{
			get => _startDate ?? "";
			set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
		}

		public string EndDate
		{
			get => _endDate ?? "";
			set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
		}

		public decimal TotalAmount
		{
			get => _totalAmount;
			set { _totalAmount = value; OnPropertyChanged(nameof(TotalAmount)); }
		}

		public string Status
		{
			get => _status ?? "";
			set
			{
				_status = value;
				OnPropertyChanged(nameof(Status));
				OnPropertyChanged(nameof(StatusColor));
				OnPropertyChanged(nameof(ShowDetailsButton));
				OnPropertyChanged(nameof(ShowCancelButton));
			}
		}

		public string CarName
		{
			get => _carName ?? "";
			set { _carName = value; OnPropertyChanged(nameof(CarName)); }
		}

		public int DaysCount
		{
			get => _daysCount;
			set { _daysCount = value; OnPropertyChanged(nameof(DaysCount)); }
		}

		public Brush StatusColor
		{
			get
			{
				string status = Status?.ToLower() ?? "";
				switch (status)
				{
					case "активен": return new SolidColorBrush(Colors.Green);
					case "завершен": return new SolidColorBrush(Colors.Gray);
					case "отменен": return new SolidColorBrush(Colors.Red);
					default: return new SolidColorBrush(Colors.Black);
				}
			}
		}

		public Visibility ShowDetailsButton => Visibility.Visible;

		public Visibility ShowCancelButton =>
			string.Equals(Status, "активен", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}