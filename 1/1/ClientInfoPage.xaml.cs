using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Npgsql;

namespace Прокат_авто
{
	public partial class ClientInfoPage : Page
	{
		private string connectionString;

		public ClientInfoPage(string connectionString)
		{
			InitializeComponent();
			this.connectionString = connectionString;
			LoadDiscounts();
			LoadPenalties();
			LoadReviews();
		}

		private void LoadDiscounts()
		{
			var discounts = new ObservableCollection<DiscountInfo>();

			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = "SELECT id_discounts, title, \" percent(%)\" FROM public.discounts ORDER BY \" percent(%)\" DESC";

					using (var cmd = new NpgsqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							discounts.Add(new DiscountInfo
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1),
								Percent = reader.GetDecimal(2),
								Description = GetDiscountDescription(reader.GetString(1))
							});
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка загрузки скидок: {ex.Message}");
				// Добавляем тестовые скидки для демонстрации
				discounts.Add(new DiscountInfo { Id = 1, Title = "Новичок", Percent = 5, Description = "Скидка 5% на первую аренду" });
				discounts.Add(new DiscountInfo { Id = 2, Title = "Постоянный клиент", Percent = 10, Description = "Скидка 10% для клиентов с более чем 5 арендами" });
				discounts.Add(new DiscountInfo { Id = 3, Title = "Долгосрочная аренда", Percent = 15, Description = "Скидка 15% при аренде от 7 дней" });
				discounts.Add(new DiscountInfo { Id = 4, Title = "Семейный тариф", Percent = 20, Description = "Скидка 20% при аренде двух автомобилей" });
			}

			discountsList.ItemsSource = discounts;
		}

		private void LoadPenalties()
		{
			var penalties = new ObservableCollection<PenaltyInfo>();

			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					// Загружаем штрафы с id 5,6,7,8
					string sql = "SELECT id_penalties, title, \"summ \" FROM public.penalties WHERE id_penalties IN (5,6,7,8) ORDER BY id_penalties";

					using (var cmd = new NpgsqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							penalties.Add(new PenaltyInfo
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1),
								Summ = reader.GetDecimal(2),
								Description = GetPenaltyDescription(reader.GetString(1))
							});
						}
					}
				}

				// Если нет данных, добавляем тестовые
				if (penalties.Count == 0)
				{
					penalties.Add(new PenaltyInfo { Id = 5, Title = "Нарушение ПДД", Summ = 5000, Description = "Штраф за нарушение правил дорожного движения" });
					penalties.Add(new PenaltyInfo { Id = 6, Title = "Повреждение автомобиля", Summ = 15000, Description = "Штраф за повреждение автомобиля (царапины, вмятины)" });
					penalties.Add(new PenaltyInfo { Id = 7, Title = "Загрязнение салона", Summ = 3000, Description = "Штраф за сильное загрязнение салона автомобиля" });
					penalties.Add(new PenaltyInfo { Id = 8, Title = "Просрочка аренды", Summ = 1000, Description = "Штраф за каждый час просрочки возврата автомобиля" });
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка загрузки штрафов: {ex.Message}");
				// Добавляем тестовые штрафы
				penalties.Add(new PenaltyInfo { Id = 5, Title = "Нарушение ПДД", Summ = 5000, Description = "Штраф за нарушение правил дорожного движения" });
				penalties.Add(new PenaltyInfo { Id = 6, Title = "Повреждение автомобиля", Summ = 15000, Description = "Штраф за повреждение автомобиля (царапины, вмятины)" });
				penalties.Add(new PenaltyInfo { Id = 7, Title = "Загрязнение салона", Summ = 3000, Description = "Штраф за сильное загрязнение салона автомобиля" });
				penalties.Add(new PenaltyInfo { Id = 8, Title = "Просрочка аренды", Summ = 1000, Description = "Штраф за каждый час просрочки возврата автомобиля" });
			}

			penaltiesList.ItemsSource = penalties;
		}

		private void LoadReviews()
		{
			var reviews = new ObservableCollection<ReviewInfo>();

			// Тестовые отзывы (можно загружать из БД при наличии таблицы reviews)
			reviews.Add(new ReviewInfo
			{
				Name = "Анна Смирнова",
				Initials = "АС",
				Rating = 5,
				Comment = "Отличный сервис! Автомобили чистые, всегда вовремя приезжают. Поддержка отвечает быстро. Рекомендую!",
				Date = "15.03.2026"
			});

			reviews.Add(new ReviewInfo
			{
				Name = "Дмитрий Волков",
				Initials = "ДВ",
				Rating = 5,
				Comment = "Пользуюсь Drive уже год, все отлично. Цены адекватные, автопарк постоянно обновляется. Особенно нравится, что есть электромобили.",
				Date = "28.02.2026"
			});

			reviews.Add(new ReviewInfo
			{
				Name = "Елена Морозова",
				Initials = "ЕМ",
				Rating = 4,
				Comment = "Хороший каршеринг, но иногда бывают проблемы с поиском свободного авто в центре города. В остальном все отлично.",
				Date = "10.02.2026"
			});

			reviews.Add(new ReviewInfo
			{
				Name = "Максим Андреев",
				Initials = "МА",
				Rating = 5,
				Comment = "Лучший каршеринг в городе! Большой выбор автомобилей, понятное приложение, быстрая регистрация. Спасибо команде Drive!",
				Date = "05.01.2026"
			});

			reviewsList.ItemsSource = reviews;
		}

		private string GetDiscountDescription(string title)
		{
			switch (title)
			{
				case "Новичок":
					return "Скидка 5% на первую аренду автомобиля";
				case "Постоянный клиент":
					return "Скидка 10% для клиентов, совершивших более 5 аренд";
				case "Долгосрочная аренда":
					return "Скидка 15% при аренде автомобиля на срок от 7 дней";
				case "Семейный тариф":
					return "Скидка 20% при аренде двух и более автомобилей одновременно";
				case "Ночной тариф":
					return "Скидка 25% на аренду в ночное время (с 00:00 до 06:00)";
				case "Выходной день":
					return "Скидка 15% на аренду в субботу и воскресенье";
				default:
					return $"Скидка {title} применяется автоматически при выполнении условий";
			}
		}

		private string GetPenaltyDescription(string title)
		{
			switch (title)
			{
				case "Нарушение ПДД":
					return "Штраф за нарушение правил дорожного движения (фиксируется камерами)";
				case "Повреждение автомобиля":
					return "Штраф за любые повреждения кузова или салона автомобиля";
				case "Загрязнение салона":
					return "Штраф за сильное загрязнение салона, требующее профессиональной химчистки";
				case "Просрочка аренды":
					return "Штраф за каждый час просрочки возврата автомобиля";
				case "Курение в салоне":
					return "Штраф за курение в салоне автомобиля";
				case "Перегруз автомобиля":
					return "Штраф за превышение допустимой нагрузки на автомобиль";
				default:
					return "Штраф начисляется при нарушении правил пользования каршерингом";
			}
		}
	}

	public class DiscountInfo
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public decimal Percent { get; set; }
		public string Description { get; set; }
	}

	public class PenaltyInfo
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public decimal Summ { get; set; }
		public string Description { get; set; }
	}

	public class ReviewInfo
	{
		public string Name { get; set; }
		public string Initials { get; set; }
		public int Rating { get; set; }
		public string Comment { get; set; }
		public string Date { get; set; }
	}
}