using System.Text.RegularExpressions;
using System.Windows;

namespace Wpf_db_008_0._2v;

public partial class CustomerWindow : Window
    {
        public Customer CustomerData { get; private set; }
        private bool isEditMode;

        public CustomerWindow(Customer customer = null)
        {
            InitializeComponent();
            isEditMode = customer != null;
            
            if (isEditMode)
            {
                CustomerData = new Customer
                {
                    CustomerID = customer.CustomerID,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    Address = customer.Address,
                    IsActive = customer.IsActive
                };
                this.Title = "Edit Customer";
            }
            else
            {
                CustomerData = new Customer { IsActive = true };
                this.Title = "Add New Customer";
            }

            this.DataContext = CustomerData;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(CustomerData.FirstName))
            {
                ShowError("First name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerData.LastName))
            {
                ShowError("Last name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerData.Email) || !IsValidEmail(CustomerData.Email))
            {
                ShowError("Please enter a valid email address.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerData.PhoneNumber) || !IsValidPhone(CustomerData.PhoneNumber))
            {
                ShowError("Please enter a valid phone number (digits only).");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerData.Address))
            {
                ShowError("Address is required.");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^\d+$");
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

