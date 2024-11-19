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
            MessageBox.Show("Please select a customer to edit.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var editWindow = new CustomerWindow(selectedCustomer);
        if (editWindow.ShowDialog() == true)
        {
            try
            {
                UpdateCustomer(editWindow.CustomerData);
                LoadCustomers(); // Refresh the grid
                MessageBox.Show("Customer updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
}


