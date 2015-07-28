using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BookstoreWPFApp{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        static private DataSet dataSet;

        private int localISBN;
        private int localAuthorID;
        private string filterAuthor;
        private string filterCity;
        private string filterState;
        private int filterRating;
        private decimal filterPrice;
        private int filterPages;

        /// <summary>
        /// Main Window
        /// </summary>
        public MainWindow(){
            InitializeComponent();
            InitializeDataSet();
            InitializeRating();
            InitializeFilters();
            InitializeDataGrid();
            InitializeCUDButtons();
        }

        /// <summary>
        /// Initialize data set
        /// </summary>
        private void InitializeDataSet(){
            dataSet = new System.Data.DataSet();

            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            List<string> storedProcCommandList = new List<string>(){"GetAllAuthors", "GetAllBooks",
                                                                    "GetAllBooksAuthors", "GetAllBooksGenres",
                                                                    "GetAllCities", "GetAllGenres", "GetAllStates"};

            // ADO .NET disconnected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                foreach (string storedProcCommand in storedProcCommandList){
                    using (SqlCommand command = new SqlCommand(storedProcCommand, conn)){
                        command.CommandType = CommandType.StoredProcedure;
                        string tableName = storedProcCommand.Substring(6);

                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command)){
                            DataTable bookstoreTable = new DataTable(tableName);

                            adapter.Fill(bookstoreTable);
                            conn.Close();
                            dataSet.Tables.Add(bookstoreTable);
                        }
                        conn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Initialize rating
        /// </summary>
        private void InitializeRating(){
            filterRating = 1;

            rbtn1.IsChecked = true;
            chkboxLeast.IsChecked = true;
        }

        /// <summary>
        /// Initialize filters
        /// </summary>
        private void InitializeFilters(){
            filterCity = "All";
            filterState = "All";
            filterPrice = 0;
            filterPages = 0;

            // Filter Drop Downs
            InitializeDropDown("City", "Cities", ref cboxCity, "All");
            InitializeDropDown("State", "States", ref cboxState, "All");
            InitializeDropDown("FirstName", "LastName", "Authors", ref cboxAuthor);
            InitializeDropDown("Genre", "Genres", ref cboxGenre, "All");
            cboxCover.ItemsSource = new List<string>(){ "All", "Hard", "Soft" };

            // CUD Drop Downs
            InitializeDropDown("Genre", "Genres", ref cboxNewGenre, "All");
            InitializeDropDown("City", "Cities", ref cboxNewCity, "All");
            cboxNewRating.ItemsSource = new List<string>() { "1", "2", "3", "4", "5" };
            cboxNewCover.ItemsSource = new List<string>() { "Hard", "Soft" };
        }

        /// <summary>
        /// Initialize drop down
        /// </summary>
        private void InitializeDropDown(string columnHeader, string tableName, ref ComboBox cbox, string selection) {
            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;

            string selectCommand;
            if (cbox.Name.ToString().Equals("cboxCity") && !selection.Equals("All"))
                selectCommand = String.Format("SELECT City FROM Bookstore.dbo.Cities INNER JOIN Bookstore.dbo.States ON Cities.StateID = States.StateID WHERE States.State = '{0}'", selection);
            else
                selectCommand = String.Format("SELECT {0} FROM Bookstore.dbo.{1}", columnHeader, tableName);

            // ADO .NET connected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand(selectCommand, conn)){
                    conn.Open();

                    using (SqlDataReader reader = command.ExecuteReader()){
                        List<string> cboxList = new List<string>();

                        while (reader.Read()){
                            cboxList.Add(reader[columnHeader].ToString());
                        }

                        // Sort city, state, genre, and cover drop downs
                        if (columnHeader.Equals("City") || columnHeader.Equals("State") ||
                            columnHeader.Equals("Genre") || columnHeader.Equals("Cover"))
                            cboxList.Sort();

                        string boxName = cbox.Name.ToString();
                        if (!boxName.Equals("cboxNewGenre") && !boxName.Equals("cboxNewCity") &&
                            !boxName.Equals("cboxNewCover") && !boxName.Equals("cboxNewRating"))
                            cboxList.Insert(0, "All");
                        
                        cbox.ItemsSource = cboxList;
                    }

                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Overloaded initialize drop down
        /// </summary>
        private void InitializeDropDown(string columnHeader1, string columnHeader2, string tableName, ref ComboBox cbox) {
            if (cbox.Items.Count > 0)
                cbox.ItemsSource = null;

            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            string selectCommand = String.Format("SELECT {0}, {1} FROM Bookstore.dbo.{2}", columnHeader1, columnHeader2, tableName);

            // ADO .NET connected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand(selectCommand, conn)){
                    conn.Open();

                    using (SqlDataReader reader = command.ExecuteReader()){
                        List<string> cboxList = new List<string>();

                        while (reader.Read()){
                            cboxList.Add(String.Format("{0} {1}", reader[columnHeader1].ToString(), reader[columnHeader2].ToString()));
                        }

                        if (tableName.Equals("Authors"))
                            cboxList.Sort();

                        cboxList.Insert(0, "All");
                        cbox.ItemsSource = cboxList;
                    }

                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Initialize data grid
        /// </summary>
        private void InitializeDataGrid() {
            UpdateDataGrid("All", "All", "All", "All", "All", filterRating, filterPrice, filterPages);
        }
        
        /// <summary>
        /// Initialize CUD Buttons
        /// </summary>
        private void InitializeCUDButtons(){
            UpdateCUDButtons(false, true, false, false);
        }

        /// <summary>
        /// Update data grid
        /// </summary>        
        private void UpdateDataGrid(string author, string city, string state, string genre, string cover, int rating, decimal price, int pages){
            var query = from authors in dataSet.Tables["Authors"].AsEnumerable() join
                             booksauthors in dataSet.Tables["BooksAuthors"].AsEnumerable() on authors.Field<int>("AuthorID") equals booksauthors.Field<int>("AuthorID") join
                             books in dataSet.Tables["Books"].AsEnumerable() on booksauthors.Field<int>("ISBN") equals books.Field<int>("ISBN") join
                             booksgenres in dataSet.Tables["BooksGenres"].AsEnumerable() on books.Field<int>("ISBN") equals booksgenres.Field<int>("ISBN") join
                             genres in dataSet.Tables["Genres"].AsEnumerable() on booksgenres.Field<int>("GenreID") equals genres.Field<int>("GenreID") join
                             cities in dataSet.Tables["Cities"].AsEnumerable() on books.Field<int>("CityID") equals cities.Field<int>("CityID") join
                             states in dataSet.Tables["States"].AsEnumerable() on cities.Field<int>("StateID") equals states.Field<int>("StateID")
                        select new { Title = books.Field<string>("Title"), FirstName = authors.Field<string>("FirstName"), LastName = authors.Field<string>("LastName"),
                                     City = cities.Field<string>("City"), State = states.Field<string>("State"), Genre = genres.Field<string>("Genre"),
                                     Cover = books.Field<string>("Cover"), Rating = books.Field<int>("Rating"), Price = books.Field<decimal>("Price"),
                                     Pages = books.Field<int>("Pages") };
            
            var filteredQuery = query;
            if (!author.Equals("All"))
                filteredQuery = filteredQuery.Where(f => (String.Format("{0} {1}", f.FirstName, f.LastName)).Equals(author));

            if (city.Equals("All") && state.Equals("All")){
                InitializeDropDown("City", "Cities", ref cboxCity, "All");
                InitializeDropDown("State", "States", ref cboxState, "All");
            } else if (!city.Equals("All") && state.Equals("All")){
                if (filterCity.Equals(city))
                    InitializeDropDown("City", "Cities", ref cboxCity, "All");
                cboxCity.SelectedValue = city;
                if (cboxCity.SelectedValue == null)
                   cboxCity.SelectedIndex = 0;
                filteredQuery = filteredQuery.Where(f => f.City.Equals(city));
            } else if (city.Equals("All") && !state.Equals("All")){
                InitializeDropDown("City", "Cities", ref cboxCity, state);
                filteredQuery = filteredQuery.Where(f => f.State.Equals(state));
            } else {
                if (!filterState.Equals(state))
                    InitializeDropDown("City", "Cities", ref cboxCity, state);

                filteredQuery = filteredQuery.Where(f => f.City.Equals(city));
                filteredQuery = filteredQuery.Where(f => f.State.Equals(state));
            }

            if (!genre.Equals("All"))
                filteredQuery = filteredQuery.Where(f => f.Genre.Equals(genre));
            if (!cover.Equals("All"))
                filteredQuery = filteredQuery.Where(f => f.Cover.Equals(cover));
            if (chkboxLeast.IsChecked.Value)
                filteredQuery = filteredQuery.Where(f => f.Rating >= Convert.ToInt32(rating));
            else
                filteredQuery = filteredQuery.Where(f => f.Rating.Equals(Convert.ToInt32(rating)));
            if (!price.Equals(0))
                filteredQuery = filteredQuery.Where(f => f.Price >= price);
            if (!pages.Equals(0))
                filteredQuery = filteredQuery.Where(f => f.Pages >= pages);

            filterCity = city;
            filterState = state;
            datagrid.ItemsSource = filteredQuery;
        }

        /// <summary>
        /// Update CUD Buttons
        /// </summary>
        private void UpdateCUDButtons(bool clearStatus, bool createStatus, bool updateStatus, bool deleteStatus){
            btnDeselect.IsEnabled = clearStatus;
            btnCreate.IsEnabled = createStatus;
            btnUpdate.IsEnabled = updateStatus;
            btnDelete.IsEnabled = deleteStatus;
        }

        /// <summary>
        /// Update Modify Options
        /// </summary>
        private void UpdateModifyOptions(ref IList dataList){
            string[] rowData = new string[]{};
            
            if (dataList.Count > 0){
                if (dataList.Count == 1){
                    rowData = dataList[0].ToString().Split(new Char[] { '{', ',', '}' }, StringSplitOptions.RemoveEmptyEntries);
                    UpdateCUDFields(ref rowData);
                    UpdateCUDButtons(true, false, true, true);
                }
                else {
                    UpdateCUDFields(ref rowData);
                    UpdateCUDButtons(true, false, false, true);                    
                }
            } else {
                UpdateCUDFields(ref rowData);
                UpdateCUDButtons(false, true, false, false);
            }
        }

        /// <summary>
        /// Update CUD Fields
        /// </summary> 
        private void UpdateCUDFields(ref string[] rowData){
            if (rowData.Length == 0) {
                txtboxNewTitle.Text = "";
                txtboxNewAuthor.Text = "";
                txtboxNewPages.Text = "";
                txtboxNewPrice.Text = "";
                cboxNewGenre.SelectedValue = null;
                cboxNewCity.SelectedValue = null;
                cboxNewRating.SelectedValue = null;
                cboxNewCover.SelectedValue = null;
            } else {
                txtboxNewTitle.Text = rowData[0].Substring(9);
                txtboxNewAuthor.Text = String.Format("{0} {1}", rowData[1].Substring(13), rowData[2].Substring(12));
                txtboxNewPages.Text = rowData[9].Substring(9);
                txtboxNewPrice.Text = rowData[8].Substring(9);
                cboxNewGenre.SelectedValue = rowData[5].Substring(9);
                cboxNewCity.SelectedValue = rowData[3].Substring(8);
                cboxNewRating.SelectedValue = rowData[7].Substring(10);
                cboxNewCover.SelectedValue = rowData[6].Substring(9);
            }
        }

        /// <summary>
        /// Check for valid filter options before update
        /// </summary> 
        private void CheckFilters(){
            if (cboxAuthor.SelectedValue != null && cboxCity.SelectedValue != null &&
                cboxState.SelectedValue != null && cboxGenre.SelectedValue != null &&
                cboxCover.SelectedValue != null)
                UpdateDataGrid(cboxAuthor.SelectedValue.ToString(), cboxCity.SelectedValue.ToString(),
                               cboxState.SelectedValue.ToString(), cboxGenre.SelectedValue.ToString(),
                               cboxCover.SelectedValue.ToString(), filterRating, filterPrice, filterPages);
        }

        /// <summary>
        /// Check for valid modify fields
        /// </summary> 
        private bool CheckModifyFields(){
            if (!txtboxNewTitle.Text.Trim().Length.Equals(0) && !txtboxNewAuthor.Text.Trim().Length.Equals(0) &&
                !txtboxNewPages.Text.Trim().Length.Equals(0) && !txtboxNewPrice.Text.Trim().Length.Equals(0) &&
                cboxNewGenre.SelectedIndex >= 0 && cboxNewCity.SelectedIndex >= 0 &&
                cboxNewRating.SelectedIndex >= 0 && cboxNewCover.SelectedIndex >= 0){
                return true;
            } else
                return false;
        }

        /// <summary>
        /// Update data grid on drop down change
        /// </summary> 
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e){
            CheckFilters();
        }

        /// <summary>
        /// Update data grid on rating check box change
        /// </summary> 
        private void chkboxLeast_CheckChanged(object sender, RoutedEventArgs e){
            CheckFilters();
        }

        /// <summary>
        /// Update data grid on rating change
        /// </summary> 
        private void Rating_CheckChanged(object sender, RoutedEventArgs e){
            RadioButton check = sender as RadioButton;
            if (check != null && check.IsChecked.Value){
                filterRating = Convert.ToInt32(check.Content);
                CheckFilters();
            }
        }

        /// <summary>
        /// Update data grid on price of page changed
        /// </summary> 
        private void txtboxFilter_TextChanged(object sender, TextChangedEventArgs e){
            TextBox txtbox = (TextBox)sender;

            if (txtbox.Text == ""){
                if (txtbox.Name.ToString().Equals("txtboxPrice")){
                    filterPrice = 0;
                    CheckFilters();
                } else
                    filterPages = 0;
                return;
            }

            if (txtbox.Name.ToString().Equals("txtboxPrice")){
                try{
                    filterPrice = Decimal.Parse(txtboxPrice.Text);
                    CheckFilters();
                } catch (Exception){
                    MessageBox.Show("Error: \"" + txtboxPrice.Text + "\" is not a valid price.");
                    txtbox.Text = "";
                    return;
                }
            } else {
                try{
                    filterPages = Convert.ToInt32(txtboxPages.Text);
                    CheckFilters();
                } catch (Exception){
                    MessageBox.Show("Error: \"" + txtboxPages.Text + "\" is not a valid page number.");
                    txtbox.Text = "";
                    return;
                }
            }
        }

        /// <summary>
        /// Update data grid on price of page changed
        /// </summary> 
        private void txtboxModify_TextChanged(object sender, TextChangedEventArgs e){
            TextBox txtbox = (TextBox)sender;

            if (txtbox.Text == "")
                return;
            else if (txtbox.Name.ToString().Equals("txtboxNewPrice")){
                try{
                    filterPrice = Decimal.Parse(txtbox.Text);
                } catch (Exception){
                    MessageBox.Show("Error: \"" + txtbox.Text + "\" is not a valid price.");
                    txtbox.Text = "";
                    return;
                }
            } else {
                try{
                    filterPages = Convert.ToInt32(txtbox.Text);
                } catch (Exception){
                    MessageBox.Show("Error: \"" + txtbox.Text + "\" is not a valid page number.");
                    txtbox.Text = "";
                    return;
                }
            }
        }

        /// <summary>
        /// Reset filter settings
        /// </summary>
        private void btnReset_Click(object sender, RoutedEventArgs e){
            filterCity = "All";
            filterState = "All";
            filterRating = 1;
            filterPrice = 0;
            filterPages = 0;
            
            cboxAuthor.SelectedIndex = 0;
            cboxCity.SelectedIndex = 0;
            cboxState.SelectedIndex = 0;
            cboxCity.SelectedIndex = 0;
            cboxGenre.SelectedIndex = 0;
            cboxCover.SelectedIndex = 0;
            rbtn1.IsChecked = true;
            chkboxLeast.IsChecked = true;
            txtboxPrice.Text = "";
            txtboxPages.Text = "";

            CheckFilters();
        }

        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e){
            IList dataList = ((DataGrid)sender).SelectedItems;
            UpdateModifyOptions(ref dataList);
        }

        /// <summary>
        /// Clear modify settings
        /// </summary>
        private void btnDeselect_Click(object sender, RoutedEventArgs e){
            datagrid.UnselectAll();
        }

        /// <summary>
        /// Click create button
        /// </summary>
        private void btnCreate_Click(object sender, RoutedEventArgs e){
            if (!CheckModifyFields()){
                MessageBox.Show("Error: Please fill out all modify fields.");
                return;
            }

            filterAuthor = cboxAuthor.SelectedValue.ToString();
            if (txtboxLog.Text.Equals(""))
                txtboxLog.Text = String.Format("{0} Succeeded in creating new book '{1}'.", System.DateTime.Now.ToString(), txtboxNewTitle.Text);
            else
                txtboxLog.Text = String.Format("{0}{1}{2}Succeeded in creating new book '{3}'.", txtboxLog.Text, Environment.NewLine, System.DateTime.Now.ToString(), txtboxNewTitle.Text);

            CreateLocalBook();
            CreateLocalAuthor();
            CreateLocalBookAuthor();
            CreateLocalBookGenre();

            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            Dictionary<string, string> cmdList = new Dictionary<string, string>(){ {"Books", CreateDBBook()},
                                                                                   {"Authors", CreateDBAuthor()},
                                                                                   {"BooksAuthors", CreateDBBookAuthor()},
                                                                                   {"BooksGenres", CreateDBBookGenre()} };

            // ADO .NET disconnected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                foreach (KeyValuePair<string, string> cmd in cmdList){
                    using (SqlCommand command = new SqlCommand()){
                        command.CommandText = cmd.Value;

                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter()){
                            adapter.InsertCommand = command;
                            adapter.InsertCommand.Connection = conn;
                            adapter.Update(dataSet, cmd.Key);
                            conn.Close();
                        }
                        conn.Close();
                    }
                }
            }

            CheckFilters();

            InitializeDropDown("FirstName", "LastName", "Authors", ref cboxAuthor);
            cboxAuthor.SelectedValue = filterAuthor;
            datagrid.UnselectAll();
        }

        /// <summary>
        /// Click delete button
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e){
            IList dataList = datagrid.SelectedItems;

            if (dataList.Count > 1){
                if (txtboxLog.Text.Equals(""))
                    txtboxLog.Text = String.Format("{0} ERROR - Multi-line DELETE not yet implemented.", System.DateTime.Now.ToString());
                else
                    txtboxLog.Text = String.Format("{0}{1}{2} ERROR - Multi-line DELETE not yet implemented.", txtboxLog.Text, Environment.NewLine, System.DateTime.Now.ToString(), txtboxNewTitle.Text);
                datagrid.UnselectAll();
                return;
            }


            if (txtboxLog.Text.Equals(""))
                txtboxLog.Text = String.Format("{0} Succeeded in deleting book '{1}'.", System.DateTime.Now.ToString(), txtboxNewTitle.Text);
            else
                txtboxLog.Text = String.Format("{0}{1}{2} Succeeded in deleting book '{3}'.", txtboxLog.Text, Environment.NewLine, System.DateTime.Now.ToString(), txtboxNewTitle.Text);

            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            Dictionary<string, string> cmdList = new Dictionary<string, string>(){ {"BooksAuthors", DeleteDBBookAuthor()},
                                                                                   {"BooksGenres", DeleteDBBookGenre()},
                                                                                   {"Books", DeleteDBBook()} };

            DeleteLocalBookAuthor();
            DeleteLocalBookGenre();
            DeleteLocalBook();

            // ADO .NET disconnected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                foreach (KeyValuePair<string, string> cmd in cmdList){
                    using (SqlCommand command = new SqlCommand()){
                        command.CommandText = cmd.Value;

                        conn.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter()){
                            adapter.DeleteCommand = command;
                            adapter.DeleteCommand.Connection = conn;
                            adapter.DeleteCommand.ExecuteNonQuery();
                            conn.Close();
                        }
                        conn.Close();
                    }
                }
            }

            UpdateDBAuthor();

            InitializeDropDown("FirstName", "LastName", "Authors", ref cboxAuthor);
            cboxAuthor.SelectedIndex = 0;
            CheckFilters();
        }

        /// <summary>
        /// Check if author has no books
        /// </summary>
        private void UpdateDBAuthor(){
            string query = String.Format("DELETE FROM Bookstore.dbo.Authors " +
                                         "WHERE Bookstore.dbo.Authors.AuthorID NOT IN " +
                                         "(SELECT Bookstore.dbo.[Books Authors].AuthorID " +
                                         " FROM Bookstore.dbo.[Books Authors])");
            
            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            // ADO .NET disconnected architecture
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand()){
                    command.CommandText = query;

                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter()){
                        adapter.DeleteCommand = command;
                        adapter.DeleteCommand.Connection = conn;
                        adapter.DeleteCommand.ExecuteNonQuery();
                        conn.Close();
                    }
                    conn.Close();
                }
            }

            UpdateLocalAuthor();
        }

        /// <summary>
        /// Create Local Book
        /// </summary>
        private void CreateLocalBook(){
            DataTable isbnTable = new DataTable();
            
            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand("SELECT MAX(ISBN) FROM Bookstore.dbo.Books", conn)){
                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command)){
                        adapter.Fill(isbnTable);
                        conn.Close();
                    }
                    conn.Close();
                }
            }

            DataRow newBooksRow = dataSet.Tables["Books"].NewRow();
            localISBN = Convert.ToInt32(isbnTable.Rows[0].ItemArray[0])+1;
            
            newBooksRow["ISBN"] = localISBN;
            newBooksRow["Title"] = txtboxNewTitle.Text.ToString();
            newBooksRow["CityID"] = GetCityID();
            newBooksRow["Pages"] = Convert.ToInt32(txtboxNewPages.Text);
            newBooksRow["Price"] = Decimal.Parse(txtboxNewPrice.Text);
            newBooksRow["Cover"] = cboxNewCover.SelectedValue.ToString();
            newBooksRow["Rating"] = Convert.ToInt32(cboxNewRating.SelectedValue.ToString());
            dataSet.Tables["Books"].Rows.Add(newBooksRow);

        }

        /// <summary>
        /// Delete Local Book
        /// </summary>
        private void DeleteLocalBook(){
            DataTable books = dataSet.Tables["Books"];
            for (int i=books.Rows.Count-1; i>=0; --i ){
                DataRow row = books.Rows[i];
                if (row["Title"].ToString().Equals(txtboxNewTitle.Text.ToString()) &&
                    row["CityID"].ToString().Equals(GetCityID().ToString()) &&
                    Convert.ToInt32(row["Pages"]) == Convert.ToInt32(txtboxNewPages.Text) &&
                    Decimal.Parse(row["Price"].ToString()) == Decimal.Parse(txtboxNewPrice.Text) &&
                    row["Cover"].ToString().Equals(cboxNewCover.SelectedValue.ToString()) &&
                    Convert.ToInt32(row["Rating"]) == Convert.ToInt32(cboxNewRating.SelectedValue.ToString())) {
                        books.Rows.Remove(row);
                }
            }
        }

        /// <summary>
        /// Create Local Author
        /// </summary>
        private void CreateLocalAuthor(){
            DataTable authoridTable = new DataTable();
            
            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand("SELECT MAX(AuthorID) FROM Bookstore.dbo.Authors", conn)){
                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command)){
                        adapter.Fill(authoridTable);
                        conn.Close();
                    }
                    conn.Close();
                }
            }

            string[] name = txtboxNewAuthor.Text.ToString().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            DataRow newAuthorsRow = dataSet.Tables["Authors"].NewRow();
            localAuthorID = Convert.ToInt32(authoridTable.Rows[0].ItemArray[0]) + 1;
            
            newAuthorsRow["AuthorID"] = localAuthorID;
            newAuthorsRow["FirstName"] = name[0];
            newAuthorsRow["LastName"] = name[1];
            dataSet.Tables["Authors"].Rows.Add(newAuthorsRow);
        }

        /// <summary>
        /// Update Local Author
        /// </summary>
        private void UpdateLocalAuthor(){
            // ADO .NET disconnected architecture
            string connString = ConfigurationManager.ConnectionStrings["BookstoreConnString"].ConnectionString;
            string storedProcCommand = "GetAllAuthors";
            using (SqlConnection conn = new SqlConnection(connString)){
                using (SqlCommand command = new SqlCommand(storedProcCommand, conn)){
                    command.CommandType = CommandType.StoredProcedure;
                    string tableName = storedProcCommand.Substring(6);

                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command)){
                        DataTable bookstoreTable = new DataTable(tableName);

                        adapter.Fill(bookstoreTable);
                        conn.Close();
                        dataSet.Tables.Remove("Authors");
                        dataSet.Tables.Add(bookstoreTable);
                    }
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Create Local Book Author
        /// </summary>
        private void CreateLocalBookAuthor(){
            DataRow newBooksAuthorsRow = dataSet.Tables["BooksAuthors"].NewRow();
            newBooksAuthorsRow["ISBN"] = dataSet.Tables["Books"].Rows[dataSet.Tables["Books"].Rows.Count-1].ItemArray[0];
            newBooksAuthorsRow["AuthorID"] = dataSet.Tables["Authors"].Rows[dataSet.Tables["Authors"].Rows.Count-1].ItemArray[0];
            dataSet.Tables["BooksAuthors"].Rows.Add(newBooksAuthorsRow);
        }

        /// <summary>
        /// Delete Local Book Author
        /// </summary>
        private void DeleteLocalBookAuthor(){
            DataTable booksauthors = dataSet.Tables["BooksAuthors"];
            for (int i=booksauthors.Rows.Count-1; i>=0; --i ){
                DataRow row = booksauthors.Rows[i];
                if (Convert.ToInt32(row["ISBN"]) == Convert.ToInt32(GetISBN()) &&
                    Convert.ToInt32(row["AuthorID"]) == Convert.ToInt32(GetAuthorID())) {
                        booksauthors.Rows.Remove(row);
                }
            }
        }

        /// <summary>
        /// Create Local Book Genre
        /// </summary>
        private void CreateLocalBookGenre(){
            DataRow newBooksGenresRow = dataSet.Tables["BooksGenres"].NewRow();
            newBooksGenresRow["ISBN"] = dataSet.Tables["Books"].Rows[dataSet.Tables["Books"].Rows.Count - 1].ItemArray[0];
            newBooksGenresRow["GenreID"] = GetGenreID();
            dataSet.Tables["BooksGenres"].Rows.Add(newBooksGenresRow);
        }

        /// <summary>
        /// Delete Local Book Genre
        /// </summary>
        private void DeleteLocalBookGenre(){
            DataTable booksgenres = dataSet.Tables["BooksGenres"];
            for (int i=booksgenres.Rows.Count-1; i>=0; --i ){
                DataRow row = booksgenres.Rows[i];
                if (Convert.ToInt32(row["ISBN"]) == Convert.ToInt32(GetISBN()) &&
                    Convert.ToInt32(row["GenreID"]) == Convert.ToInt32(GetGenreID())) {
                        booksgenres.Rows.Remove(row);
                }
            }
        }

        /// <summary>
        /// Create DB Book
        /// </summary>
        private string CreateDBBook(){
            string command = String.Format("INSERT INTO Bookstore.dbo.Books (ISBN, Title, CityID, Pages, Price, Cover, Rating) " +
                                           "VALUES ({0}, '{1}', {2}, {3}, {4}, '{5}', {6})",
                                           localISBN, txtboxNewTitle.Text.ToString(), GetCityID(), Convert.ToInt32(txtboxNewPages.Text.ToString()),
                                           Decimal.Parse(txtboxNewPrice.Text.ToString()), cboxNewCover.SelectedValue.ToString(), Convert.ToInt32(cboxNewRating.SelectedValue.ToString()));
            return command;
        }

        /// <summary>
        /// Delete DB Book
        /// </summary>
        private string DeleteDBBook(){
            string command = String.Format("DELETE FROM Bookstore.dbo.Books " +
                                           "WHERE Bookstore.dbo.Books.Title = '{0}' " +
                                           "AND Bookstore.dbo.Books.CityID = {1} " +
                                           "AND Bookstore.dbo.Books.Pages = {2} " +
                                           "AND Bookstore.dbo.Books.Price = {3} " +
                                           "AND Bookstore.dbo.Books.Cover = '{4}' " +
                                           "AND Bookstore.dbo.Books.Rating = {5}", 
                                           txtboxNewTitle.Text.ToString(), GetCityID(), Convert.ToInt32(txtboxNewPages.Text.ToString()),
                                           Decimal.Parse(txtboxNewPrice.Text.ToString()), cboxNewCover.SelectedValue.ToString(), Convert.ToInt32(cboxNewRating.SelectedValue.ToString()));
            return command;
        }

        /// <summary>
        /// Create DB Author
        /// </summary>
        private string CreateDBAuthor(){
            string[] name = txtboxNewAuthor.Text.ToString().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = String.Format("INSERT INTO Bookstore.dbo.Authors (AuthorID, FirstName, LastName) " +
                                           "VALUES ({0}, '{1}', '{2}')",
                                           localAuthorID, name[0], name[1]);
            return command;
        }

        /// <summary>
        /// Create DB Book Authors
        /// </summary>
        private string CreateDBBookAuthor(){
            string command = String.Format("INSERT INTO Bookstore.dbo.[Books Authors] (ISBN, AuthorID) " +
                                           "VALUES ({0}, {1})",
                                           dataSet.Tables["Books"].Rows[dataSet.Tables["Books"].Rows.Count - 1].ItemArray[0],
                                           dataSet.Tables["Authors"].Rows[dataSet.Tables["Authors"].Rows.Count - 1].ItemArray[0]);
            return command;
        }

        /// <summary>
        /// Delete DB Book Authors
        /// </summary>
        private string DeleteDBBookAuthor(){
            string command = String.Format("DELETE FROM Bookstore.dbo.[Books Authors] " +
                                           "WHERE Bookstore.dbo.[Books Authors].ISBN = {0} " +
                                           "AND Bookstore.dbo.[Books Authors].AuthorID = {1}",
                                           GetISBN(),
                                           GetAuthorID());
            return command;
        }

        /// <summary>
        /// Create DB Book Genres
        /// </summary>
        private string CreateDBBookGenre(){
            string command = String.Format("INSERT INTO Bookstore.dbo.[Books Genres] (ISBN, GenreID) " +
                                           "VALUES ({0}, {1})",
                                           dataSet.Tables["Books"].Rows[dataSet.Tables["Books"].Rows.Count - 1].ItemArray[0],
                                           GetGenreID());
            return command;
        }

        /// <summary>
        /// Delete DB Book Genres
        /// </summary>
        private string DeleteDBBookGenre(){
            string command = String.Format("DELETE FROM Bookstore.dbo.[Books Genres] " +
                                           "WHERE Bookstore.dbo.[Books Genres].ISBN = {0} " +
                                           "AND Bookstore.dbo.[Books Genres].GenreID = {1}",
                                           GetISBN(),
                                           GetGenreID());
            return command;
        }

        /// <summary>
        /// Get City ID
        /// </summary>
        private int GetCityID(){
            DataTable dataTable;

            var query = from cities in dataSet.Tables["Cities"].AsEnumerable()
                        where cities.Field<string>("City").Equals(cboxNewCity.SelectedValue.ToString())
                        select cities;
            dataTable = query.CopyToDataTable();
            int cityid = Convert.ToInt32(dataTable.Rows[0].ItemArray[0]);

            return cityid;
        }

        /// <summary>
        /// Get ISBN
        /// </summary>
        private int GetISBN() {
            DataTable dataTable;

            var query = from books in dataSet.Tables["Books"].AsEnumerable()
                        where books.Field<string>("Title").Equals(txtboxNewTitle.Text)
                        select books;
            dataTable = query.CopyToDataTable();
            int isbn = Convert.ToInt32(dataTable.Rows[0].ItemArray[0]);

            return isbn;
        }

        /// <summary>
        /// Get Max ISBN
        /// </summary>
        private int GetMaxISBN() {
            var query = from books in dataSet.Tables["Books"].AsEnumerable()
                        select books.Field<int>("ISBN");
            int maxISBN = query.Max();
            return maxISBN;
        }

        /// <summary>
        /// Get AuthorID
        /// </summary>
        private int GetAuthorID() {
            DataTable dataTable;

            var query = from authors in dataSet.Tables["Authors"].AsEnumerable()
                        where String.Format("{0} {1}", authors.Field<string>("FirstName"), authors.Field<string>("LastName")).Equals(txtboxNewAuthor.Text)
                        select authors;
            dataTable = query.CopyToDataTable();
            int authorid = Convert.ToInt32(dataTable.Rows[0].ItemArray[0]);

            return authorid;
        }

        /// <summary>
        /// Get Max AuthorID
        /// </summary>
        private int GetMaxAuthorID() {
            var query = from authors in dataSet.Tables["Authors"].AsEnumerable()
                        select authors.Field<int>("AuthorID");
            int GetMaxAuthorID = query.Max();
            return GetMaxAuthorID;
        }

        /// <summary>
        /// Get Genre ID
        /// </summary>
        private int GetGenreID() {
            DataTable dataTable;

            var query = from genres in dataSet.Tables["Genres"].AsEnumerable()
                        where genres.Field<string>("Genre").Equals(cboxNewGenre.SelectedValue.ToString())
                        select genres;
            dataTable = query.CopyToDataTable();
            int genreid = Convert.ToInt32(dataTable.Rows[0].ItemArray[0]);

            return genreid;
        }

        /// <summary>
        /// Check textbox in NewAuthor
        /// </summary>
        private void NewAuthor_TextCheck(object sender, RoutedEventArgs e){
            string[] name = txtboxNewAuthor.Text.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (name.Length != 2){
                MessageBox.Show("Error: \"" + txtboxNewAuthor.Text + "\" is not a valid first and last name.");
                txtboxNewAuthor.Text = "";
            }
        }

        /// <summary>
        /// Save log
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e){
            string[] logLines = txtboxLog.Text.Split(new Char[] { '"', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            try{
                FileStream fileStream = File.Open("BookstoreLog.txt", FileMode.Append, FileAccess.Write);
                StreamWriter fileWriter = new StreamWriter(fileStream);
                fileWriter.WriteLine(System.DateTime.Now.ToString());
                foreach (string line in logLines)
                    fileWriter.WriteLine(line);
                fileWriter.Flush();
                fileWriter.Close();
            }
            catch (IOException ioe){
                Console.WriteLine(ioe);
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e){
            if (txtboxLog.Text.Equals(""))
                txtboxLog.Text = String.Format("{0} ERROR: Update not yet implemented.", System.DateTime.Now.ToString());
            else
                txtboxLog.Text = String.Format("{0}{1}{2} ERROR: Update not yet implemented.", txtboxLog.Text, Environment.NewLine, System.DateTime.Now.ToString());
            datagrid.UnselectAll();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) {
            txtboxLog.Text = "";
        }
    }
}