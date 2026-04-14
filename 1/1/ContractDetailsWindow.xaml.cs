using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;

namespace Прокат_авто
{
	public partial class ContractDetailsWindow : Window
	{
		private int contractId;
		private string connectionString;
		private decimal totalAmount = 0;
		private decimal totalDiscount = 0;
		private decimal totalPenalties = 0;
		private decimal totalPaid = 0;

		public ContractDetailsWindow(int contractId, string connectionString)
		{
			InitializeComponent();
			this.contractId = contractId;
			this.connectionString = connectionString;
			LoadContractDetails();
		}

		private void LoadContractDetails()
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					// Загружаем основную информацию о договоре (без сотрудника)
					string contractSql = @"
                        SELECT 
                            c.id_contract,
                            c.""start date"",
                            c.""end date"",
                            c.""rental amount"",
                            c.""total amount"",
                            COALESCE(s.title, 'Неизвестно') as status_title,
                            car.stamp,
                            car.model
                        FROM public.contract c
                        LEFT JOIN public.starus_of_the_agreement s ON c.id_starus_of_the_agreement = s.id_starus_of_the_agreement
                        LEFT JOIN public.car car ON c.id_car = car.id_car
                        WHERE c.id_contract = @contractId";

					using (var cmd = new NpgsqlCommand(contractSql, conn))
					{
						cmd.Parameters.AddWithValue("@contractId", contractId);
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								DateTime startDate = reader.GetDateTime(1);
								DateTime endDate = reader.GetDateTime(2);
								int days = (endDate - startDate).Days;
								totalAmount = reader.GetDecimal(4);

								txtContractTitle.Text = $"Договор №{reader.GetInt32(0)}";
								txtContractStatus.Text = $"Статус: {reader.GetString(5)}";
								txtCarName.Text = $"{reader.GetString(6)} {reader.GetString(7)}";
								txtStartDate.Text = startDate.ToShortDateString();
								txtEndDate.Text = endDate.ToShortDateString();
								txtDaysCount.Text = days.ToString();
								txtRentalAmount.Text = $"{reader.GetDecimal(3):F2} руб.";
								txtTotalAmount.Text = $"{totalAmount:F2} руб.";
							}
						}
					}

					// Загружаем скидки
					LoadDiscounts(conn);

					// Загружаем штрафы
					LoadPenalties(conn);

					// Загружаем платежи
					LoadPayments(conn);

					// Рассчитываем остаток
					CalculateRemainingAmount();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки деталей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadDiscounts(NpgsqlConnection conn)
		{
			string sql = @"
                SELECT d.title, d."" percent(%)""
                FROM public.discounts d
                INNER JOIN public.contract c ON c.id_discounts = d.id_discounts
                WHERE c.id_contract = @contractId";

			var discounts = new ObservableCollection<DiscountItem>();

			using (var cmd = new NpgsqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@contractId", contractId);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						decimal percent = reader.GetDecimal(1);
						discounts.Add(new DiscountItem
						{
							Title = reader.GetString(0),
							Percent = percent
						});
						totalDiscount += totalAmount * (percent / 100);
					}
				}
			}

			discountsList.ItemsSource = discounts;
			txtTotalDiscount.Text = $"Общая скидка: {totalDiscount:F2} руб.";
		}

		private void LoadPenalties(NpgsqlConnection conn)
		{
			string sql = @"
                SELECT p.title, p.""summ ""
                FROM public.penalties p
                INNER JOIN public.contract c ON c.id_penalties = p.id_penalties
                WHERE c.id_contract = @contractId";

			var penalties = new ObservableCollection<PenaltyItem>();

			using (var cmd = new NpgsqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@contractId", contractId);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						decimal summ = reader.GetDecimal(1);
						penalties.Add(new PenaltyItem
						{
							Title = reader.GetString(0),
							Summ = summ
						});
						totalPenalties += summ;
					}
				}
			}

			penaltiesList.ItemsSource = penalties;
			txtTotalPenalties.Text = $"Общая сумма штрафов: {totalPenalties:F2} руб.";
		}

		private void LoadPayments(NpgsqlConnection conn)
		{
			string sql = @"
                SELECT 
                    p.payment_date,
                    p.amount,
                    COALESCE(pm.title, 'Не указан') as payment_method,
                    COALESCE(t.title, 'Не указан') as payment_type
                FROM public.payment p
                INNER JOIN public.""contract-payment"" cp ON cp.id_payment = p.id_payment
                LEFT JOIN public.payment_method pm ON p.id_payment_method = pm.""id_ payment method""
                LEFT JOIN public.type t ON p.id_type = t.id_tipe
                WHERE cp.id_contract = @contractId
                ORDER BY p.payment_date DESC";

			var payments = new ObservableCollection<PaymentItem>();

			using (var cmd = new NpgsqlCommand(sql, conn))
			{
				cmd.Parameters.AddWithValue("@contractId", contractId);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						decimal amount = reader.GetDecimal(1);
						totalPaid += amount;

						payments.Add(new PaymentItem
						{
							PaymentDate = reader.GetDateTime(0).ToShortDateString(),
							Amount = $"{amount:F2} руб.",
							PaymentMethod = reader.GetString(2),
							PaymentType = reader.GetString(3)
						});
					}
				}
			}

			paymentsGrid.ItemsSource = payments;
		}

		private void CalculateRemainingAmount()
		{
			decimal amountWithDiscountsAndPenalties = totalAmount - totalDiscount + totalPenalties;
			decimal remainingAmount = amountWithDiscountsAndPenalties - totalPaid;

			if (remainingAmount <= 0)
			{
				txtFinalAmount.Text = "0.00 руб. (Оплачено полностью)";
				txtFinalAmount.Foreground = System.Windows.Media.Brushes.LightGreen;
			}
			else
			{
				txtFinalAmount.Text = $"{remainingAmount:F2} руб.";
				txtFinalAmount.Foreground = System.Windows.Media.Brushes.White;
			}
		}

		private void btnAddPayment_Click(object sender, RoutedEventArgs e)
		{
			var addPaymentWindow = new AddPaymentWindow(contractId, connectionString);
			addPaymentWindow.Owner = this;

			if (addPaymentWindow.ShowDialog() == true)
			{
				// Обновляем данные после добавления платежа
				totalPaid = 0;
				totalDiscount = 0;
				totalPenalties = 0;

				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					LoadDiscounts(conn);
					LoadPenalties(conn);
					LoadPayments(conn);
					CalculateRemainingAmount();
				}
			}
		}
	}

	public class DiscountItem
	{
		public string Title { get; set; }
		public decimal Percent { get; set; }
	}

	public class PenaltyItem
	{
		public string Title { get; set; }
		public decimal Summ { get; set; }
	}

	public class PaymentItem
	{
		public string PaymentDate { get; set; }
		public string Amount { get; set; }
		public string PaymentMethod { get; set; }
		public string PaymentType { get; set; }
	}
}