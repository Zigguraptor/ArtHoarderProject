using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ArtHoarderClient.Infrastructure;

public class MyDataGrid : DataGrid
{
    public MyDataGrid ()
        {
            this.SelectionChanged += CustomDataGrid_SelectionChanged;
        }

        void CustomDataGrid_SelectionChanged (object sender, SelectionChangedEventArgs e)
        {
            this.SelectedItemsList = this.SelectedItems;
        }
        #region SelectedItemsList

        public IList SelectedItemsList
        {
            get { return (IList)GetValue (SelectedItemsListProperty); }
            set { SetValue (SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register ("SelectedItemsList", typeof (IList), typeof (MyDataGrid), new PropertyMetadata (null));

        #endregion
    
}