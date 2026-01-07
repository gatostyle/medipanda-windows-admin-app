using medipanda_windows_admin.Models.Response;
using medipanda_windows_admin.Services;
using System.Windows;
using System.Windows.Controls;

namespace medipanda_windows_admin.Windows
{
    public enum DialogMode
    {
        Settlement,  // 정산파일 변환
        RateTable    // 요율표 업로드
    }

    public partial class SelectDrugCompanyDialog : Window
    {
        private readonly DrugCompanyService _drugCompanyService;

        public DrugCompanyResponse? SelectedDrugCompany { get; private set; }
        public int SelectedYear { get; private set; }
        public int SelectedMonth { get; private set; }

        public SelectDrugCompanyDialog(DialogMode mode = DialogMode.Settlement)
        {
            InitializeComponent();
            _drugCompanyService = new DrugCompanyService();

            ApplyMode(mode);

            DrugCompanyListBox.SelectionChanged += (s, e) => UpdateConfirmButton();
            MonthComboBox.SelectionChanged += (s, e) => UpdateConfirmButton();

            InitializeYearMonth();

            Loaded += async (s, e) => await LoadDrugCompanies();
        }

        private void ApplyMode(DialogMode mode)
        {
            switch (mode)
            {
                case DialogMode.Settlement:
                    Title = "정산파일 변환";
                    MonthLabel.Text = "정산월 선택";
                    break;
                case DialogMode.RateTable:
                    Title = "요율표 업로드";
                    MonthLabel.Text = "적용월 선택";
                    break;
            }
        }

        private void InitializeYearMonth()
        {
            var now = DateTime.Now;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            // 년도: 작년, 올해
            var years = new List<int>();
            if (currentMonth == 1)
            {
                // 1월이면 작년만
                //years.Add(currentYear - 1);
                years.Add(currentYear - 1);
                years.Add(currentYear);
            }
            else
            {
                // 그 외에는 작년, 올해
                years.Add(currentYear - 1);
                years.Add(currentYear);
            }

            YearComboBox.ItemsSource = years;
            YearComboBox.SelectedItem = currentYear;
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem == null) return;

            var selectedYear = (int)YearComboBox.SelectedItem;
            var now = DateTime.Now;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            var months = new List<int>();

            if (selectedYear == currentYear)
            {
                // 올해면 현재 월 - 1까지
                for (int i = 1; i <= currentMonth; i++)
                {
                    months.Add(i);
                }
            }
            else
            {
                // 작년이면 1~12
                for (int i = 1; i <= 12; i++)
                {
                    months.Add(i);
                }
            }

            MonthComboBox.ItemsSource = months;

            // 기본 선택: 마지막 월
            if (months.Count > 0)
            {
                MonthComboBox.SelectedItem = months.Last();
            }
        }

        private void UpdateConfirmButton()
        {
            ConfirmButton.IsEnabled = DrugCompanyListBox.SelectedItem != null
                                      && MonthComboBox.SelectedItem != null;
        }

        private async Task LoadDrugCompanies()
        {
            try
            {
                var companies = await _drugCompanyService.GetDrugCompaniesAsync();
                DrugCompanyListBox.ItemsSource = companies;
                LoadingText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LoadingText.Text = "불러오기 실패";
                MessageBox.Show($"제약사 목록을 불러오는데 실패했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedDrugCompany = DrugCompanyListBox.SelectedItem as DrugCompanyResponse;
            SelectedYear = (int)YearComboBox.SelectedItem;
            SelectedMonth = (int)MonthComboBox.SelectedItem;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}