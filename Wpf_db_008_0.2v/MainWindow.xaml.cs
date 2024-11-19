using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private string connectionString = "Server=(LocalDb)\\MSSQLLocalDB;Database=forGenerateRandomDate;Trusted_Connection=True;MultipleActiveResultSets=True;";
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
    }

    public class Customer
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }