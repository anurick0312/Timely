using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Process_Monitor_Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        HashSet<string> allApps = new HashSet<string>();
        HashSet<string> allowed = new HashSet<string>();
        HashSet<string> excluded = new HashSet<string>();

        private IAsyncOperation<IUICommand> dialogTask;

        string data_dir_path = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                txtCurrDir.Text = folder.Path;
                data_dir_path = folder.Path;
                PopulateDataAsync();
            }
            //else
            //{
            //    this.textBlock.Text = "Operation cancelled.";
            //}
        }

        private async void PopulateDataAsync()
        {

            try
            {
                string data_path = data_dir_path + "\\data.csv";
                StorageFile file = await StorageFile.GetFileFromPathAsync(data_path);
                using (CsvParse.CsvFileReader csvReader = new CsvParse.CsvFileReader(await file.OpenStreamForReadAsync()))
                {
                    CsvParse.CsvRow row = new CsvParse.CsvRow();
                    while (csvReader.ReadRow(row))
                    {
                        string newRow = "";
                        // we'll assign this to our UI ListView
                        if (row[1].StartsWith("1"))
                            Debug.WriteLine(CsvRowToString(row));
                        allApps.Add(row[1]);
                    }
                }

                Debug.WriteLine("allApps count: " + allApps.Count);

                string exc_path = data_dir_path + "\\exclusions.txt";
                file = await StorageFile.GetFileFromPathAsync(exc_path);
                using (CsvParse.CsvFileReader csvReader = new CsvParse.CsvFileReader(await file.OpenStreamForReadAsync()))
                {
                    CsvParse.CsvRow row = new CsvParse.CsvRow();
                    while (csvReader.ReadRow(row))
                    {
                        string newRow = "";
                        // we'll assign this to our UI ListView
                        excluded.Add(row[0]);
                    }
                }

                Debug.WriteLine("exclusion count: " + excluded.Count);


                allowed = new HashSet<string>(allApps,allApps.Comparer);
                allowed.ExceptWith(excluded);

                Debug.WriteLine("allApps count: " + allApps.Count);
                Debug.WriteLine("allowed count: " + allowed.Count);

                RefreshLists(false);


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        private string CsvRowToString(CsvParse.CsvRow r)
        {
            string st = "";
            foreach (string s in r)
                st += s + " , ";
            return st;
        }

        private void BtnExclude_Click(object sender, RoutedEventArgs e)
        {
            if (listAllow.SelectedItem != null) {
                string SelectedItem = listAllow.SelectedItem.ToString();
                //Debug.WriteLine(SelectedItem);
                allowed.Remove(SelectedItem);
                excluded.Add(SelectedItem);

                RefreshLists(true);
                
            }
        }

        private void RefreshLists(bool scroll)
        {
            listAllow.Items.Clear();
            foreach (string s in allowed)
                listAllow.Items.Add(s);
            listExcl.Items.Clear();
            foreach (string s in excluded)
                listExcl.Items.Add(s);

            txtAllowedHeading.Text = "Apps currently in monitor list (" + listAllow.Items.Count + " applications)";
            txtExclHeading.Text = "Apps to be excluded from monitoring (" + listExcl.Items.Count + " applications)";

            if (scroll)
            {
                listAllow.ScrollIntoView(listAllow.Items[listAllow.Items.Count - 1]);
                listExcl.ScrollIntoView(listExcl.Items[listExcl.Items.Count - 1]);
            }
        }

        private async void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (data_dir_path == null)
            {
                ShowPopUpMessage("Please specify the data directory using the directory picker first");
                return;
            }
            string exc_path = data_dir_path + "\\exclusions.txt";
            StorageFile file = await StorageFile.GetFileFromPathAsync(exc_path);
            StringBuilder sb = new StringBuilder();
            foreach(string s in excluded)
            {
                sb.AppendLine(s);
            }
            await FileIO.WriteTextAsync(file, sb.ToString());
            PopulateDataAsync();
        }

        private void BtnAllow_Click(object sender, RoutedEventArgs e)
        {
            if (listExcl.SelectedItem != null)
            {
                string SelectedItem = listExcl.SelectedItem.ToString();
                //Debug.WriteLine(SelectedItem);
                excluded.Remove(SelectedItem);
                allowed.Add(SelectedItem);

                RefreshLists(true);

            }
        }

        private async void ShowPopUpMessage(string message)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(message);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                "OK",
                new UICommandInvokedHandler(DismissDialog)));

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            // Show the message dialog
            dialogTask = messageDialog.ShowAsync();
        }

        private void DismissDialog(IUICommand command)
        {
            dialogTask.Cancel();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (data_dir_path == null)
            {
                return;
            }
            PopulateDataAsync();
        }
    }
}
