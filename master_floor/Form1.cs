using Npgsql;
using System;
using System.Windows.Forms;


namespace master_floor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            string name_login = textBox1.Text;
            string password = textBox2.Text;

            using (var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=master_floor;Username=postgres;Password=root;"))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;

                    cmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = '" + name_login + "' AND password = '" + password + "'";


                    int count = 0;
                    object result = cmd.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out count))
                    {
                        if (count > 0)
                        {
                            MessageBox.Show("Вы успешно вошли в средство манипуляции над базой данных Мастер Пол!");
                            Form form2 = new Form2();
                            this.Hide();    
                            form2.Show();     
                        }
                        else
                        {
                            MessageBox.Show("Проверьте логин и пароль: кажется вы ввели неверное имя пользователя или пароль.");
                        }
                    }
                    conn.Close();
                }
            }
        }
    }
}