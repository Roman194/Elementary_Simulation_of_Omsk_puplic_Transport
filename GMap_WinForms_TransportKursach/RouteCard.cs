using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMap_WinForms_TransportKursach
{
    public partial class RouteCard : UserControl
    {
        public RouteCard()
        {
            InitializeComponent();
        }

        public string RouteName
        {
            get { return routeName.Text; }
            set { routeName.Text = value; }
        }

        public string Bus18
        {
            get { return bus18.Text; }
            set { bus18.Text = value; }
        }

        public string Bus36
        {
            get { return bus36.Text; }
            set { bus36.Text = value; }
        }

        public string Bus60
        {
            get { return bus60.Text; }
            set { bus60.Text = value; }
        }

        public String Bus115
        {
            get { return bus115.Text; }
            set { bus115.Text = value; }
        }

        public string Trolleybus
        {
            get { return trolleybus.Text; }
            set { trolleybus.Text = value; }
        }

        public string Tram
        {
            get { return tram.Text; }
            set { tram.Text = value; }
        }

        public string Traffic
        {
            get { return traffic.Text; }
            set { traffic.Text = value; }
        }

        public string Occupancy
        {
            get { return occupancy.Text; }
            set { occupancy.Text = value; }
        }

        private void RouteCard_Load(object sender, EventArgs e)
        {
            
        }
    }
}
