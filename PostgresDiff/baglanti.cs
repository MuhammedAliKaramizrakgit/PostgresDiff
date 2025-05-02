using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

using Asyasoft.Data.Context;
using System.Reflection;
using System.Data.Common;

using System.ComponentModel;

using System.Net;

using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

using System.Windows.Forms;

namespace Asyasoft.Data.Context
{

    public interface IBaglanti : IDisposable
    {

       
        DataSet DataSetAl(string sql);
    
        int Exec(string sql);
        object ObjectScalar(string sql);
        int Scalar(string sql);


    }

    public class baglanti : IBaglanti
    {
       
        private static Random rndomkes = new Random();
        //public static baglanti staticnotifybag;
        // public static baglanti static105bag = new baglanti(105);
        protected string ip;
        protected string username;
        static int? gerigun = 2;
        string connstring ;
        //"Host=localhost;Username=postgres;Password=antasya;Database=postgres";
        public NpgsqlConnection conn;
        public bool cartayicekti = false;
        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            cartayicekti = true;
            if (!this.disposed)
            {
                if (disposing)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
            this.disposed = true;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public baglanti(string connstring )
        {
            this.connstring = connstring;
            conn = new NpgsqlConnection(connstring);
           
        }

       



        public void bagac()
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
        }
        
    







        public DataSet DataSetAl(String sql)
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                DataSet d = new DataSet();
                if (conn.State == ConnectionState.Broken | conn.State == ConnectionState.Closed)
                    bagac();
                try
                {
                    NpgsqlDataAdapter Nd = new NpgsqlDataAdapter(sql, conn);
                    Nd.SelectCommand.CommandTimeout = 90000;
                    Nd.Fill(d);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Hata DataSetAl -> " + e.Message);
                    Console.WriteLine("Hata DataSetAl -> " + sql);
                    // Clipboard.SetText(sql);
                }
                conn.Close();
                return d;

            }
            catch (Exception)
            {

                conn.Close();
                throw;
            }

        }

        
        public object ObjectScalar(string sql)
        {
            object sonuc = null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                NpgsqlCommand cmdChk = new NpgsqlCommand(sql, conn);
                // cmdChk.Parameters.AddWithValue("sid", 1);
                //cmdChk.CommandType = CommandType.StoredProcedure;



                //  NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);

                try
                {
                    sonuc = cmdChk.ExecuteScalar();

                }
                catch (Exception e)
                {

                    //mami  Console.WriteLine(e.Message);
                    //mami  Console.WriteLine(sql);
                    //Clipboard.SetText(sql);

                    throw;
                }
                return sonuc;
            }
            catch (Exception e)
            {
                Console.WriteLine("Apooo Sıctın" + e.Message);
                return null;
            }
        }
        public int Scalar(string sql)
        {
            return Convert.ToInt32(ObjectScalar(sql) ?? -1);
        }
        public int Exec(string sql)
        {
            int sonuc = 1;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);

                try
                {
                    sonuc = cmd.ExecuteNonQuery();


                }
                catch (Exception e)
                {
                    //mami  Console.WriteLine(e.Message);
                    //mami  Console.WriteLine(sql);
                    // Clipboard.SetText(sql);
                    sonuc = -1;
                }
                return sonuc;
            }
            catch (Exception e)
            {
                Console.WriteLine("Apoo sıctın exec--" + e.InnerException ?? e.Message);
                return sonuc;
            }

        }
    }
    
   
    public static class GenelStaticler
    {
       

        public static List<int> hibeIdListesi = new List<int>();
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool AreYouInVisualStudio
        {
            get
            {
                return System.Diagnostics.Debugger.IsAttached;
            }
        }
       
       



       

    }
}