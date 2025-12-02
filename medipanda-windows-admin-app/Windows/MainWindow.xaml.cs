using medipanda_windows_admin.Converters;
using medipanda_windows_admin.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace medipanda_windows_admin.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task RunWithProgressAsync(string message, Func<Action<string>, Task> action)
        {
            var progressDialog = new ProgressDialog(message);
            progressDialog.Owner = this;

            try
            {
                var task = Task.Run(async () =>
                {
                    await action((msg) => progressDialog.UpdateMessage(msg));
                });

                progressDialog.Loaded += async (s, e) =>
                {
                    await task;
                    progressDialog.Close();
                };

                progressDialog.ShowDialog();

                await task; // 예외 전파
            }
            catch
            {
                if (progressDialog.IsVisible)
                    progressDialog.Close();
                throw;
            }
        }

        private async void ConvertSettlement_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectDrugCompanyDialog(DialogMode.Settlement);
            dialog.Owner = this;

            if (dialog.ShowDialog() != true || dialog.SelectedDrugCompany == null)
                return;

            var selectedCompany = dialog.SelectedDrugCompany!;
            var selectedYear = dialog.SelectedYear;
            var selectedMonth = dialog.SelectedMonth;
            var settlementMonth = $"{selectedYear.ToString().Substring(2, 2)}.{selectedMonth:D2}";

            var openFileDialog = new OpenFileDialog
            {
                Title = "정산파일 선택",
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var filePath = openFileDialog.FileName;
            var directory = Path.GetDirectoryName(filePath)!;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var convertedFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_converted{extension}");

            try
            {
                ISettlementConverter converter = selectedCompany.Name switch
                {
                    "국제약품(주)" => new KukjeSettlementConverter(),
                    "영진약품" => new YoungjinSettlementConverter(),
                    "(주)동구바이오제약" => new DongguSettlementConverter(),
                    _ => throw new NotSupportedException($"지원하지 않는 제약사입니다: {selectedCompany.Name}")
                };

                await RunWithProgressAsync("정산파일 변환 중...", async (updateMessage) =>
                {
                    converter.SettlementMonth = settlementMonth;
                    converter.DrugCompanyName = selectedCompany.Name;

                    updateMessage("파일 로드 중...");
                    converter.LoadFile(filePath);

                    updateMessage("데이터 파싱 중...");
                    await converter.ParseAsync();

                    updateMessage("변환 파일 저장 중...");
                    converter.SaveConverted(convertedFilePath);

                    converter.Dispose();
                });

                MessageBox.Show($"{selectedYear}년 {selectedMonth}월 정산파일 변환이 완료되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UploadKimsData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "KIMS 데이터 파일 선택",
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var filePath = openFileDialog.FileName;

            try
            {
                await RunWithProgressAsync("KIMS 데이터 업로드 중...", async (updateMessage) =>
                {
                    updateMessage("파일 업로드 중...");
                    await KimsService.Instance.UploadKimsAsync(filePath);
                });

                MessageBox.Show("KIMS 데이터 업로드가 완료되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"업로드 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UploadRateTable_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectDrugCompanyDialog(DialogMode.RateTable);
            dialog.Owner = this;

            if (dialog.ShowDialog() != true || dialog.SelectedDrugCompany == null)
                return;

            var selectedCompany = dialog.SelectedDrugCompany!;
            var selectedYear = dialog.SelectedYear;
            var selectedMonth = dialog.SelectedMonth;
            var applyMonth = $"{selectedYear}.{selectedMonth:D2}";

            var openFileDialog = new OpenFileDialog
            {
                Title = "요율표 파일 선택",
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var filePath = openFileDialog.FileName;
            int rowCount = 0;

            try
            {
                BaseRateConverter converter = selectedCompany.Name switch
                {
                    "(주)동구바이오제약" => new DongguRateConverter(),
                    "국제약품(주)" => new KukjeRateConverter(),
                    "영진약품" => new YoungjinRateConverter(),
                    "한국파마" => new HankookRateConverter(),
                    "에이치엘비제약(주)" => new HlbRateConverter(),
                    _ => throw new NotSupportedException($"지원하지 않는 제약사입니다: {selectedCompany.Name}")
                };

                await RunWithProgressAsync("요율표 업로드 중...", async (updateMessage) =>
                {
                    using (converter)
                    {
                        converter.DrugCompanyName = selectedCompany.Name;
                        converter.ApplyMonth = applyMonth;

                        updateMessage("파일 로드 중...");
                        converter.LoadFile(filePath);

                        updateMessage("데이터 파싱 중...");
                        await converter.ParseAsync();

                        rowCount = converter.Data.Rows.Count;

                        updateMessage($"서버 업로드 중... ({rowCount}건)");
                        await RateTableService.Instance.UploadRateTableAsync(converter.Data, selectedYear, selectedMonth);
                    }
                });

                MessageBox.Show(
                    $"{selectedCompany.Name} {selectedYear}년 {selectedMonth}월 요율표 업로드가 완료되었습니다.\n총 {rowCount}건",
                    "완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"업로드 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}