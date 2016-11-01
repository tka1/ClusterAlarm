using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToTwitter;
using Npgsql;
using System.Net.Mail;
using System.Net;


namespace ClusterAlarm
{
    class Program
    {

        public static void Log(string logMessage, StreamWriter w)
        {
            w.Write("\r\n");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
            DateTime.Now.ToLongDateString());
            w.WriteLine("  :");
            w.WriteLine("  {0}", logMessage);
            w.WriteLine("-------------------------------");
        }

        private static void Log(Exception e, StreamWriter w)
        {
            // throw new NotImplementedException();
            w.Write("\r\n");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
            DateTime.Now.ToLongDateString());
            w.WriteLine("  :");
            w.WriteLine("  {0}", e);
            w.WriteLine("-------------------------------");
        }
        static void Main(string[] args)
        {

            string dbserver = System.Configuration.ConfigurationManager.AppSettings["dbserver"];
            string database = System.Configuration.ConfigurationManager.AppSettings["database"];
            string userid = System.Configuration.ConfigurationManager.AppSettings["userid"];
            string password = System.Configuration.ConfigurationManager.AppSettings["password"];
            NpgsqlConnection conn = new NpgsqlConnection("Server=" + dbserver + ";Port=5432"+ ";User Id=" + userid + ";Password=Saturnus1!" + ";Database=" + database + ";");
            const string accessToken = "xxxxxxxxxxxxxxxxxxx";
            const string accessTokenSecret = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            const string consumerKey = "xxxxxxxxxxxxxxxxxxxxxxxxxx";
            const string consumerSecret = "xxxxxxxxxxxxxxxxxxxxxx";
            //const string twitterAccountToDisplay = "xxxxxxxxxxxx";
            string message = "";
            string title = "";
            string time = "";
            string call = "";
            string freq = "";
            string band = "";
            string country = "";
            string mode = "";

            var authorizer = new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = consumerKey,
                    ConsumerSecret = consumerSecret,
                    OAuthToken = accessToken,
                    OAuthTokenSecret = accessTokenSecret
                }
            };

            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log("Started", w);

            }
            while (true)
            {
                try

                {
                     message = "";
                    conn.Open();
                    NpgsqlCommand cmd2 = new NpgsqlCommand("SELECT  distinct title, dxcall, freq, band, country, mode, dx_continent,skimmode FROM cluster.alert_new_country", conn);
                    //cmd2.Parameters.Add(new NpgsqlParameter("value1", NpgsqlTypes.NpgsqlDbType.Text));
                    // cmd2.Parameters[0].Value = callsign;
                    // string dxcountry = (String)cmd2.ExecuteScalar();
                    NpgsqlDataReader dr2 = cmd2.ExecuteReader();

                    //string dx_cont = "";
                    while (dr2.Read())
                    {
                        title = dr2[0].ToString();
                        call = dr2[1].ToString();
                        freq = dr2[2].ToString();
                        band = dr2[3].ToString();
                        country = dr2[4].ToString();
                        mode = dr2[5].ToString();
                        message = "de " + title + " "  + call + " " + freq.Trim() + "KHz" + " " + band + " " + mode + " " + country;
                        Console.WriteLine(message);
                        conn.Close();

                        using (StreamWriter w = File.AppendText("new_countries.txt"))
                        {
                            w.Write("\r\n");
                            w.Write(message);
                        }
                    
                        try
                        {
                            conn.Open();

                            NpgsqlCommand cmd = new NpgsqlCommand("insert into cluster.alarms(call,time,freq,band,mode,country) values ( :value1 ,:value2,:value3,:value4,:value5,:value6)", conn);
                            cmd.Parameters.Add(new NpgsqlParameter("value1", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value2", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value3", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value4", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value5", NpgsqlTypes.NpgsqlDbType.Text));
                            cmd.Parameters.Add(new NpgsqlParameter("value6", NpgsqlTypes.NpgsqlDbType.Text));

                            cmd.Parameters[0].Value = call;
                            cmd.Parameters[1].Value = time;
                            cmd.Parameters[2].Value = freq;
                            cmd.Parameters[3].Value = band;
                            cmd.Parameters[4].Value = mode;
                            cmd.Parameters[5].Value = country;
                            NpgsqlDataReader dr = cmd.ExecuteReader();
                            conn.Close();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            conn.Close();
                        };
                        Console.WriteLine(message);
                    }

                    using (var context = new TwitterContext(authorizer))
                    {
                        if (call != "")
                        {

                           // var tweet = context.TweetAsync(message).Result;
                        }
                    };



                    conn.Close();

                }
                catch (Exception e)
                {
                     Console.WriteLine(e);
                    using (StreamWriter w = File.AppendText("log.txt"))
                    {
                        Log(e, w);
                    }
                    conn.Close();
                }
                //Console.WriteLine(message);

                string smtpAddress = "smtp.gmail.com";
                    int portNumber = 587;
                    bool enableSSL = true;
                    string emailFrom = "xxxxxxxxxxxxxxxxxxxxx";
                    string pass = "xxxxxxxxxxxxxxxx";
                    string emailTo = "xxxxxxxxxxxxxxxxx";
                    string subject = message;
                    string body = message;

                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(emailFrom);
                        mail.To.Add(emailTo);
                        mail.Subject = subject;
                        mail.Body = body;
                        mail.IsBodyHtml = false;

                        using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                        {
                            smtp.Credentials = new NetworkCredential(emailFrom, pass);
                            smtp.EnableSsl = enableSSL;
                            smtp.Timeout = 3000;
                            try
                            {
                                if (message != "")
                                {
                                   smtp.Send(mail);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                               // Console.ReadKey();
                            }
                        }
                    }

            
                    System.Threading.Thread.Sleep(59000);
                

            }


        }
    }
}
