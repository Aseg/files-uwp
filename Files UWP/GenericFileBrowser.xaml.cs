﻿using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Navigation;
using Files.Interacts;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace Files
{
    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock emptyTextGFB;
        public TextBlock textBlock;
        public DataGrid data;
        public MenuFlyout context;
        public MenuFlyout emptySpaceContext;
        public MenuFlyout HeaderContextMenu;
        public Page GFBPageName;
        public ContentDialog AddItemBox;
        public ContentDialog NameBox;
        public TextBox inputFromRename;
        public string inputForRename;
        public Flyout CopiedFlyout;
        public Grid grid;
        public ProgressBar progressBar;
        public ItemViewModel<GenericFileBrowser> instanceViewModel;
        public Interaction<GenericFileBrowser> instanceInteraction;
        public EmptyFolderTextState TextState { get; set; } = new EmptyFolderTextState();

        public GenericFileBrowser()
        {
            this.InitializeComponent();
            GFBPageName = GenericItemView;
            emptyTextGFB = EmptyText;
            progressBar = progBar;
            progressBar.Visibility = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            grid = RootGrid;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            RefreshEmptySpace.Click += NavigationActions.Refresh_Click;
            
        }

        private void SelectAllAcceleratorDG_Invoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            //await AddDialog.ShowAsync();
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView packageView = Clipboard.GetContent();
                if (packageView.Contains(StandardDataFormats.StorageItems))
                {
                    App.PS.isEnabled = true;
                }
                else
                {
                    App.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                App.PS.isEnabled = false;
            }

        }


        
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            instanceViewModel = new ItemViewModel<GenericFileBrowser>();
            instanceInteraction = new Interaction<GenericFileBrowser>();
            PasteEmptySpace.Click += instanceInteraction.PasteItem_ClickAsync;
            data.ItemsSource = instanceViewModel.FilesAndFolders;
            OpenItem.Click += instanceInteraction.OpenItem_Click;
            ShareItem.Click += instanceInteraction.ShareItem_Click;
            DeleteItem.Click += instanceInteraction.DeleteItem_Click;
            RenameItem.Click += instanceInteraction.RenameItem_Click;
            CutItem.Click += instanceInteraction.CutItem_Click;
            CopyItem.Click += instanceInteraction.CopyItem_ClickAsync;
            SidebarPinItem.Click += instanceInteraction.PinItem_Click;
            AllView.RightTapped += instanceInteraction.AllView_RightTapped;
            AllView.DoubleTapped += instanceInteraction.List_ItemClick;

            var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
            CurrentInstance.BackButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoBack;
            CurrentInstance.ForwardButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoForward;
            CurrentInstance.RefreshButton.IsEnabled = true;
            Clipboard_ContentChanged(null, null);
            CurrentInstance.AlwaysPresentCommands.isEnabled = true;
            var parameters = (string)eventArgs.Parameter;
            instanceViewModel.CancelLoadAndClearFiles();
            instanceViewModel.Universal.path = parameters;
            CurrentInstance.AddItemButton.Click += AddItem_Click;

            TextState.isVisible = Visibility.Collapsed;

            instanceViewModel.AddItemsToCollectionAsync(instanceViewModel.Universal.path, this);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                CurrentInstance.PathText.Text = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                CurrentInstance.PathText.Text = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                CurrentInstance.PathText.Text = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                CurrentInstance.PathText.Text = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                CurrentInstance.PathText.Text = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                CurrentInstance.PathText.Text = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                CurrentInstance.PathText.Text = "Videos";
            }
            else
            {
                CurrentInstance.PathText.Text = parameters;
            }

            // Reset DataGrid Rows that may be in "cut" command mode
            instanceInteraction.dataGridRows.Clear();
            Interaction<GenericFileBrowser>.FindChildren<DataGridRow>(instanceInteraction.dataGridRows, (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).GFBPageName.Content);
            foreach (DataGridRow dataGridRow in instanceInteraction.dataGridRows)
            {
                if (data.Columns[0].GetCellContent(dataGridRow).Opacity < 1)
                {
                    data.Columns[0].GetCellContent(dataGridRow).Opacity = 1;
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if(instanceViewModel._fileQueryResult != null)
            {
                instanceViewModel._fileQueryResult.ContentsChanged -= instanceViewModel.FileContentsChanged;
            }
        }


        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            
        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                    foreach (IStorageItem item in await e.DataView.GetStorageItemsAsync())
                    {
                        if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            instanceInteraction.CloneDirectoryAsync((item as StorageFolder).Path, instanceViewModel.Universal.path, (item as StorageFolder).DisplayName);
                        }
                        else
                        {
                            await (item as StorageFile).CopyAsync(await StorageFolder.GetFolderFromPathAsync(instanceViewModel.Universal.path));
                        }
                    }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.progressBar.Visibility = Visibility.Collapsed;
        }

        private async void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            var newCellText = (data.SelectedItem as ListedItem)?.FileName;
            var selectedItem = instanceViewModel.FilesAndFolders[e.Row.GetIndex()];
            if(selectedItem.FileType == "Folder")
            {
                StorageFolder folderToRename = await StorageFolder.GetFolderFromPathAsync(selectedItem.FilePath);
                if(folderToRename.DisplayName != newCellText)
                {
                    try
                    {
                        await folderToRename.RenameAsync(newCellText);
                    }
                    catch (Exception)
                    {
                        MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                        await itemAlreadyExistsDialog.ShowAsync();
                    }
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
            else
            {
                StorageFile fileToRename = await StorageFile.GetFileFromPathAsync(selectedItem.FilePath);
                if (fileToRename.DisplayName != newCellText)
                {
                    if (newCellText.StartsWith("."))
                    {
                        MessageDialog unsupportedRenameDialog = new MessageDialog("Items starting with \".\" are not supported. you'll need to remove the illegal character and try again.", "Unsupported Item Name");
                        await unsupportedRenameDialog.ShowAsync();
                        return;
                    }
                    else if (!newCellText.Contains("."))
                    {
                        MessageDialog unsupportedRenameDialog = new MessageDialog("The name you're attempting to rename this file to is missing a file extension. You'll need to determine this and try again.", "Specify a File Extension");
                        await unsupportedRenameDialog.ShowAsync();
                        return;
                    }
                    else
                    {
                        try
                        {
                            await fileToRename.RenameAsync(newCellText);
                        }
                        catch (Exception)
                        {
                            MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                            await itemAlreadyExistsDialog.ShowAsync();
                        }
                    }
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AllView.CommitEdit();
            data.SelectedItem = null;
            ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().HomeItems.isEnabled = false;
            ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().ShareItems.isEnabled = false;
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            if(e.AddedItems.Count > 0)
            {
                ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().HomeItems.isEnabled = true;
                ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().ShareItems.isEnabled = true;

            }
            else if(data.SelectedItems.Count == 0)
            {
                ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().HomeItems.isEnabled = false;
                ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>().ShareItems.isEnabled = false;
            }
        }

        private void NameDialog_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private void NameDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void AllView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
        }

        private void AllView_DragLeave(object sender, DragEventArgs e)
        {
            
        }

        private void RightClickContextMenu_Opened(object sender, object e)
        {
            var selectedDataItem = instanceViewModel.FilesAndFolders[AllView.SelectedIndex];
            if (selectedDataItem.FileType != "Folder" || AllView.SelectedItems.Count > 1)
            {
                SidebarPinItem.IsEnabled = false;
            }
            else if (selectedDataItem.FileType == "Folder")
            {
                SidebarPinItem.IsEnabled = true;
            }
        }
    }

    public class EmptyFolderTextState : INotifyPropertyChanged
    {


        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}