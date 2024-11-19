using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wpf_db_008_0._2v;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private string connectionString =
        "Server=(LocalDb)\\MSSQLLocalDB;Database=forGenerateRandomDate;Trusted_Connection=True;MultipleActiveResultSets=True;";

    private int currentPage = 1;
    private int itemsPerPage = 10;
    private int totalItems;
    private string searchFirstName = "";
    private string searchLastName = "";
    private bool? isActive = null;

    public event PropertyChangedEventHandler PropertyChanged;

    private ObservableCollection<Customer> customers;

    public ObservableCollection<Customer> Customers
    {
        get { return customers; }
        set
        {
            customers = value;
            OnPropertyChanged("Customers");
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Get total count for pagination
            string countQuery = @"
                    SELECT COUNT(*) 
                    FROM Customer 
                    WHERE (@FirstName = '' OR FirstName LIKE @FirstName + '%')
                    AND (@LastName = '' OR LastName LIKE @LastName + '%')
                    AND (@IsActive IS NULL OR IsActive = @IsActive)";

            using (SqlCommand cmd = new SqlCommand(countQuery, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", searchFirstName);
                cmd.Parameters.AddWithValue("@LastName", searchLastName);
                cmd.Parameters.AddWithValue("@IsActive", (object)isActive ?? DBNull.Value);

                totalItems = (int)cmd.ExecuteScalar();
            }

            // Get paginated data
            string query = @"
                    SELECT CustomerID, FirstName, LastName, Email, PhoneNumber, Address, IsActive
                    FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY CustomerID) AS RowNum, *
                        FROM Customer
                        WHERE (@FirstName = '' OR FirstName LIKE @FirstName + '%')
                        AND (@LastName = '' OR LastName LIKE @LastName + '%')
                        AND (@IsActive IS NULL OR IsActive = @IsActive)
                    ) AS CustomerPaged
                    WHERE RowNum BETWEEN @StartRow AND @EndRow";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                int startRow = (currentPage - 1) * itemsPerPage + 1;
                int endRow = startRow + itemsPerPage - 1;

                cmd.Parameters.AddWithValue("@StartRow", startRow);
                cmd.Parameters.AddWithValue("@EndRow", endRow);
                cmd.Parameters.AddWithValue("@FirstName", searchFirstName);
                cmd.Parameters.AddWithValue("@LastName", searchLastName);
                cmd.Parameters.AddWithValue("@IsActive", (object)isActive ?? DBNull.Value);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Customers = new ObservableCollection<Customer>();
                    while (reader.Read())
                    {
                        Customers.Add(new Customer
                        {
                            CustomerID = reader.GetInt32(reader.GetOrdinal("CustomerID")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        });
                    }
                }
            }
        }

        UpdatePaginationInfo();
    }

    private void UpdatePaginationInfo()
    {
        int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
        txtPaginationInfo.Text = $"Page {currentPage} of {totalPages}";
        btnPrevious.IsEnabled = currentPage > 1;
        btnNext.IsEnabled = currentPage < totalPages;
    }

    private void btnPrevious_Click(object sender, RoutedEventArgs e)
    {
        if (currentPage > 1)
        {
            currentPage--;
            LoadCustomers();
        }
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
        if (currentPage < totalPages)
        {
            currentPage++;
            LoadCustomers();
        }
    }

    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
        searchFirstName = txtSearchFirstName.Text.Trim();
        searchLastName = txtSearchLastName.Text.Trim();
        isActive = chkActive.IsChecked;
        currentPage = 1;
        LoadCustomers();
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        txtSearchFirstName.Text = "";
        txtSearchLastName.Text = "";
        chkActive.IsChecked = null;
        searchFirstName = "";
        searchLastName = "";
        isActive = null;
        currentPage = 1;
        LoadCustomers();
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        var addWindow = new CustomerWindow();
        if (addWindow.ShowDialog() == true)
        {
            try
            {
                InsertCustomer(addWindow.CustomerData);
                LoadCustomers(); // Refresh the grid
                MessageBox.Show("Customer added successfully!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding customer: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void btnEdit_Click(object sender, RoutedEventArgs e)
    {
        var selectedCustomer = customersGrid.SelectedItem as Customer;
        if (selectedCustomer == null)
        {
            MessageBox.Show("Please select a customer to edit.", "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var editWindow = new CustomerWindow(selectedCustomer);
        if (editWindow.ShowDialog() == true)
        {
            try
            {
                UpdateCustomer(editWindow.CustomerData);
                LoadCustomers(); // Refresh the grid
                MessageBox.Show("Customer updated successfully!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating customer: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void InsertCustomer(Customer customer)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = @"
                    INSERT INTO Customer (FirstName, LastName, Email, PhoneNumber, Address, IsActive)
                    VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @Address, @IsActive)";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                cmd.Parameters.AddWithValue("@Email", customer.Email);
                cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                cmd.Parameters.AddWithValue("@Address", customer.Address);
                cmd.Parameters.AddWithValue("@IsActive", customer.IsActive);

                cmd.ExecuteNonQuery();
            }
        }
    }

    private void UpdateCustomer(Customer customer)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = @"
                    UPDATE Customer 
                    SET FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email,
                        PhoneNumber = @PhoneNumber,
                        Address = @Address,
                        IsActive = @IsActive
                    WHERE CustomerID = @CustomerID";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customer.CustomerID);
                cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                cmd.Parameters.AddWithValue("@Email", customer.Email);
                cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                cmd.Parameters.AddWithValue("@Address", customer.Address);
                cmd.Parameters.AddWithValue("@IsActive", customer.IsActive);

                cmd.ExecuteNonQuery();
            }
        }
    }

    // В класі MainWindow додайте:
    private async void btnCheckUsage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Валідація
            if (!decimal.TryParse(txtDataLimit.Text, out decimal dataLimit) || dataLimit <= 0)
            {
                MessageBox.Show("Please enter a valid data limit (must be greater than 0).",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("GetUserLimitGbAndSetSuspendedNew", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@DataLimitGB", dataLimit);
                    cmd.Parameters.AddWithValue("@StartDate",
                        (object)dpStartDate.SelectedDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate",
                        (object)dpEndDate.SelectedDate ?? DBNull.Value);

                    var results = new ObservableCollection<UsageMonitorData>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new UsageMonitorData
                            {
                                CustomerID = reader.GetInt32(reader.GetOrdinal("CustomerID")),
                                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                TariffName = reader.GetString(reader.GetOrdinal("TariffName")),
                                TariffSpeed = reader.GetInt32(reader.GetOrdinal("TariffSpeed")),
                                TotalDataUsedGB = reader.GetDecimal(reader.GetOrdinal("TotalDataUsedGB")),
                                ExcessDataGB = reader.GetDecimal(reader.GetOrdinal("ExcessDataGB")),
                                DaysActive = reader.GetInt32(reader.GetOrdinal("DaysActive")),
                                AvgDailyUsageGB = reader.GetDecimal(reader.GetOrdinal("AvgDailyUsageGB")),
                                SubscriptionStatus = reader.GetString(reader.GetOrdinal("SubscriptionStatus"))
                            });
                        }
                    }

                    usageGrid.ItemsSource = results;
                    txtUsageResult.Text = $"Found {results.Count} users exceeding data limit of {dataLimit} GB. " +
                                          "Their subscriptions have been suspended.";
                }
            }
        }
        catch (SqlException ex)
        {
            MessageBox.Show($"Database error: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void btnStartTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnStartTest.IsEnabled = false;
            btnResetData.IsEnabled = false;
            txtLog.Text = "";

            // Початкове значення
            decimal initialValue = await GetCurrentUsageValue();
            txtInitialValue.Text = initialValue.ToString("F2") + " GB";
            LogMessage("Initial value: " + initialValue.ToString("F2") + " GB");

            // Запускаємо дві транзакції паралельно
            var task1 = Transaction1();
            var task2 = Transaction2();

            await Task.WhenAll(task1, task2);

            // Отримуємо фінальне значення
            decimal finalValue = await GetCurrentUsageValue();
            txtFinalValue.Text = finalValue.ToString("F2") + " GB";

            // Очікуване значення
            decimal expectedValue = initialValue + 50 + 30;
            txtExpectedValue.Text = expectedValue.ToString("F2") + " GB";

            LogMessage($"\nFinal value: {finalValue:F2} GB");
            LogMessage($"Expected value: {expectedValue:F2} GB");

            if (finalValue < expectedValue)
            {
                LogMessage("\nANOMALY DETECTED: Lost Update!");
                LogMessage("The final value is less than expected because one of the updates was lost.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnStartTest.IsEnabled = true;
            btnResetData.IsEnabled = true;
        }
    }

    private async Task<decimal> GetCurrentUsageValue()
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(
                       "SELECT DataUsed FROM InternetUsage WHERE UsageID = 1", conn))
            {
                return (decimal)await cmd.ExecuteScalarAsync();
            }
        }
    }

    private async Task Transaction1()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    BEGIN TRANSACTION;
                    DECLARE @currentUsage DECIMAL(10,2);
                    SELECT @currentUsage = DataUsed FROM InternetUsage WHERE UsageID = 1;
                    WAITFOR DELAY '00:00:02';
                    UPDATE InternetUsage SET DataUsed = @currentUsage + 50 WHERE UsageID = 1;
                    COMMIT TRANSACTION;
                    SELECT DataUsed FROM InternetUsage WHERE UsageID = 1;";

                    decimal result = (decimal)await cmd.ExecuteScalarAsync();
                    txtTransaction1.Text = result.ToString("F2") + " GB";
                    LogMessage($"Transaction 1 completed: {result:F2} GB");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Transaction 1 error: {ex.Message}");
        }
    }

    private async Task Transaction2()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    BEGIN TRANSACTION;
                    DECLARE @currentUsage DECIMAL(10,2);
                    SELECT @currentUsage = DataUsed FROM InternetUsage WHERE UsageID = 1;
                    UPDATE InternetUsage SET DataUsed = @currentUsage + 30 WHERE UsageID = 1;
                    COMMIT TRANSACTION;
                    SELECT DataUsed FROM InternetUsage WHERE UsageID = 1;";

                    decimal result = (decimal)await cmd.ExecuteScalarAsync();
                    txtTransaction2.Text = result.ToString("F2") + " GB";
                    LogMessage($"Transaction 2 completed: {result:F2} GB");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Transaction 2 error: {ex.Message}");
        }
    }

    private async void btnResetData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(
                           "UPDATE InternetUsage SET DataUsed = 100 WHERE UsageID = 1", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            txtInitialValue.Text = "---";
            txtTransaction1.Text = "---";
            txtTransaction2.Text = "---";
            txtFinalValue.Text = "---";
            txtExpectedValue.Text = "---";
            txtLog.Text = "Data reset to 100 GB";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogMessage(string message)
    {
        txtLog.Text += message + "\n";
        txtLog.ScrollToEnd();
    }

    /////////////////////////SOLVE TRANSACTION
    ///
    private async void btnStartResolutionTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnStartResolutionTest.IsEnabled = false;
            btnResetResolutionData.IsEnabled = false;
            txtResLog.Text = "";

            int maxRetries = int.Parse(txtRetryCount.Text);
            string isolationLevel = ((ComboBoxItem)cboIsolationLevel.SelectedItem).Content.ToString();

            decimal initialValue = await GetCurrentUsageValue();
            txtResInitialValue.Text = initialValue.ToString("F2") + " GB";
            LogResMessage("Initial value: " + initialValue.ToString("F2") + " GB");
            LogResMessage($"Using isolation level: {isolationLevel}");
            LogResMessage($"Max retry attempts: {maxRetries}\n");

            var task1 = ExecuteTransactionWithRetry(1, 50, isolationLevel, maxRetries);
            var task2 = ExecuteTransactionWithRetry(2, 30, isolationLevel, maxRetries);

            await Task.WhenAll(task1, task2);

            decimal finalValue = await GetCurrentUsageValue();
            decimal expectedValue = initialValue + 50 + 30;

            txtResFinalValue.Text = finalValue.ToString("F2") + " GB";
            txtResExpectedValue.Text = expectedValue.ToString("F2") + " GB";

            LogResMessage($"\nFinal value: {finalValue:F2} GB");
            LogResMessage($"Expected value: {expectedValue:F2} GB");

            if (Math.Abs(finalValue - expectedValue) < 0.01m)
            {
                LogResMessage("\nSUCCESS: Both updates were applied correctly!");
            }
            else
            {
                LogResMessage("\nWARNING: Updates were not applied as expected.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnStartResolutionTest.IsEnabled = true;
            btnResetResolutionData.IsEnabled = true;
        }
    }

    private async Task ExecuteTransactionWithRetry(int transactionNumber, decimal addValue,
        string isolationLevel, int maxRetries)
    {
        int retryCount = 0;
        bool success = false;

        while (!success && retryCount < maxRetries)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;

                        // Встановлюємо рівень ізоляції
                        cmd.CommandText = $"SET TRANSACTION ISOLATION LEVEL {isolationLevel};";
                        await cmd.ExecuteNonQueryAsync();

                        cmd.CommandText = @"
                        BEGIN TRANSACTION;
                        DECLARE @currentUsage DECIMAL(10,2);
                        SELECT @currentUsage = DataUsed FROM InternetUsage WITH (UPDLOCK) WHERE UsageID = 1;
                        WAITFOR DELAY '00:00:02';
                        UPDATE InternetUsage SET DataUsed = @currentUsage + @addValue WHERE UsageID = 1;
                        COMMIT TRANSACTION;
                        SELECT DataUsed FROM InternetUsage WHERE UsageID = 1;";

                        cmd.Parameters.AddWithValue("@addValue", addValue);

                        decimal result = (decimal)await cmd.ExecuteScalarAsync();

                        if (transactionNumber == 1)
                            txtResTransaction1.Text = $"Success after {retryCount} retries";
                        else
                            txtResTransaction2.Text = $"Success after {retryCount} retries";

                        LogResMessage(
                            $"Transaction {transactionNumber} completed (attempt {retryCount + 1}): {result:F2} GB");
                        success = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    if (transactionNumber == 1)
                        txtResTransaction1.Text = "Failed after max retries";
                    else
                        txtResTransaction2.Text = "Failed after max retries";

                    LogResMessage($"Transaction {transactionNumber} failed after {maxRetries} attempts: {ex.Message}");
                    throw;
                }

                LogResMessage($"Transaction {transactionNumber} retry {retryCount}: {ex.Message}");
                await Task.Delay(100 * retryCount); // Експоненціальна затримка
            }
        }
    }

    private async void btnResetResolutionData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(
                           "UPDATE InternetUsage SET DataUsed = 100 WHERE UsageID = 1", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            txtResInitialValue.Text = "---";
            txtResTransaction1.Text = "---";
            txtResTransaction2.Text = "---";
            txtResFinalValue.Text = "---";
            txtResExpectedValue.Text = "---";
            txtResLog.Text = "Data reset to 100 GB";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogResMessage(string message)
    {
        txtResLog.Text += message + "\n";
        txtResLog.ScrollToEnd();
    }
}
