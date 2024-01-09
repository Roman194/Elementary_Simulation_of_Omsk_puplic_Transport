using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;

using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;
using System.IO;
using System.Diagnostics;

namespace GMap_WinForms_TransportKursach
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        List<List<PointLatLng>> linesCoords = new List<List<PointLatLng>>();
        List<List<Stop>> stops = new List<List<Stop>>();
        List<Routes> routes = new List<Routes>();
        List<List<Vehicle>> vehicles = new List<List<Vehicle>>();
        List<List<int>> interval = new List<List<int>>();
        Random rand = new Random();
        GMapOverlay stopMarkersOverlay = new GMapOverlay("busStopsOv");
        GMapOverlay vehiclesMarkersOverlay = new GMapOverlay("vehOv");
        GMapOverlay linesOverlay = new GMapOverlay("polygonsOv");
        GMapOverlay selectedRouteOverlay = new GMapOverlay("selectedRouteOv");
        uint currentPage = 0;
        int highlightedRouteNumber = -1;
        int depatureInterval = 4;
        int afterStartZeroDirectionCounter = 4;
        int afterStartOneDirectionCounter = 3;
        
        int timeInterval = 2880;
        uint systemTime = 0;

        private void gMapControl1_Load(object sender, EventArgs e)
        {
            gMap.Bearing = 0;
            gMap.CanDragMap = true;
            gMap.DragButton = MouseButtons.Left;
            gMap.GrayScaleMode = true;

            gMap.MarkersEnabled = true;
            gMap.MaxZoom = 14;
            gMap.MinZoom = 12;
            gMap.Zoom = 14;
            gMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;

            gMap.NegativeMode = false;
            gMap.PolygonsEnabled = true;
            gMap.RoutesEnabled = true;

            gMap.MapProvider = OpenStreetMapProvider.Instance;
            gMap.CacheLocation = System.Windows.Forms.Application.StartupPath + @"\AppCache";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMap.Position = new PointLatLng(54.9630692160955, 73.38600260618107);

            StreamReader streamReader = new StreamReader("assets/endCells.txt");
            StreamReader routesStreamReader = new StreamReader("assets/routesEndCells.txt");

            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
        
            Workbook workbook = excel.Workbooks.Open(@"C:\Users\Test\Documents\Visual Studio 2022\Templates\ProjectTemplates\Visual C++\GMap_WinForms_TransportKursach\GMap_WinForms_TransportKursach\bin\Debug\assets\coursach.xlsx");
            Worksheet namesWorksheet = workbook.Worksheets[1];
            Worksheet routesWorksheet = workbook.Worksheets[2];
            Worksheet worksheet = workbook.Worksheets[3];

            for (int j = 0; j < 42; j++)
            {
                linesCoords.Add(new List<PointLatLng>());
                stops.Add(new List<Stop>());

                String currentCellLine = Convert.ToString(j + 3);
                Range cell = worksheet.Range["D"+currentCellLine+":"+streamReader.ReadLine()+currentCellLine];

                String stopName;
                int columnIndex = 0;
                int secondaryCoordsCount = 0;


                foreach (string result in cell.Value)
                {

                    string[] currentCoord = result.Split(new char[] { ',' });
                    for (int i = 0; i < currentCoord.Length; i++)
                        currentCoord[i] = currentCoord[i].Replace(".", ",");

                    PointLatLng stopCoords = new PointLatLng( Convert.ToDouble(currentCoord[0]), Convert.ToDouble(currentCoord[1]));
                    linesCoords[j].Add(stopCoords);

                    stopName = (string)(namesWorksheet.Cells[j + 3, columnIndex + 4] as Range).Value;

                    if (stopName != "-")
                    {
                        stopCoords.Lat -= 0.0004;
                        stops[j].Add(new Stop(new List<Passengers>(), stopName, stopCoords));

                        GMapMarker marker = new GMarkerGoogle(
                            stopCoords, 
                            new Bitmap(Image.FromFile(@"assets/stopGreenIcon.png"))
                        );

                        marker.ToolTipText = "\n" + stopName + "\nКоличество пассажиров: "+stops[j][columnIndex - secondaryCoordsCount].Passengers.Count;

                        stopMarkersOverlay.Markers.Add(marker);
                    }
                    else
                    {
                        secondaryCoordsCount++;
                    }

                    columnIndex++;
                }

                GMapRoute line = new GMapRoute(linesCoords[j], "Route1");
                line.Stroke = new Pen(Color.Green, 3);
                line.IsVisible = true;
                linesOverlay.Routes.Add(line);
            }

            

            gMap.Overlays.Add(linesOverlay);
            gMap.Overlays.Add(stopMarkersOverlay);

            RouteCard[] routeCard = new RouteCard[20];
            for (int j=0; j<60; j++)
            {
                vehicles.Add(new List<Vehicle>());
                List<StopCoords> generalCoords = new List<StopCoords>();
                var routeLineName = Convert.ToString((routesWorksheet.Cells[j + 7, 2] as Range).Value);
                var routeLineExtendedName = " " + Convert.ToString((routesWorksheet.Cells[j + 7, 3] as Range).Value);
                String currentLine = Convert.ToString(j+7);
                Range routeRange = routesWorksheet.Range["D"+currentLine+":"+routesStreamReader.ReadLine()+currentLine];

                foreach(String result in routeRange.Value)
                {
                    string[] currentStop = result.Split(new char[] { '.' });
                    generalCoords.Add(new StopCoords(Convert.ToInt32(currentStop[0]), Convert.ToInt32(currentStop[1])));
                }
                RouteInfo routeInfo = new RouteInfo(routeLineName, routeLineExtendedName, generalCoords);
                List<uint> vechileCapacityByTypes = new List<uint>();
                Range capacityRange = routesWorksheet.Range["Z"+currentLine+":AE"+currentLine];
                int iteration = 0;
                foreach(Double vehicleType in capacityRange.Value)
                {
                    uint vechileTypeCount = Convert.ToUInt32(vehicleType);
                    if(vechileTypeCount != 0)
                    {
                        for (int z=0; z<vechileTypeCount; z++)
                        {
                            int capacity = 0;
                            switch (iteration)
                            {
                                case 0: capacity = 18; break;
                                case 1: capacity = 36; break;
                                case 2: capacity = 60; break;
                                case 3: capacity = 115; break;
                                case 4: capacity = 125; break;
                                case 5: capacity = 178; break;
                            }
                            StopCoords nextGenCoords = new StopCoords(0, 0);
                            StopCoords currentStop = new StopCoords(0,0);
                            uint directn = 0;
                            if(z%2 == 0)
                            {
                                currentStop.IdLine = generalCoords[0].IdLine;
                                currentStop.IdStop = generalCoords[0].IdStop;

                                nextGenCoords.IdLine = generalCoords[1].IdLine;
                                nextGenCoords.IdStop = generalCoords[1].IdStop;
                            }
                            else
                            {
                                directn = 1;
                                currentStop.IdLine = generalCoords[generalCoords.Count - 1].IdLine;
                                currentStop.IdStop = generalCoords[generalCoords.Count - 1].IdStop;

                                nextGenCoords.IdLine = generalCoords[generalCoords.Count - 2].IdLine;
                                nextGenCoords.IdStop = generalCoords[generalCoords.Count - 2].IdStop;
                            }
                            //textBox1.Text += Convert.ToString(currentStop.IdLine+"."+currentStop.IdStop+"\n");
                            vehicles[j].Add(
                                new Vehicle(routeLineName, nextGenCoords, currentStop, new StopCoords(0,0),directn, false, false, capacity, new List<Passengers>()));
                        }
                    }
                    vechileCapacityByTypes.Add(vechileTypeCount);
                    iteration++;
                }
               
                routes.Add(new Routes(routeInfo, vechileCapacityByTypes));

                if (j < 20)
                {
                    comboBox2.Items.Add(routeLineName);

                    routeCard[j] = new RouteCard();
                    routeCard[j].RouteName = routeLineName + routeLineExtendedName;
                    routeCard[j].Bus18 = Convert.ToString(vechileCapacityByTypes[0]);
                    routeCard[j].Bus36 = Convert.ToString(vechileCapacityByTypes[1]);
                    routeCard[j].Bus60 = Convert.ToString(vechileCapacityByTypes[2]);
                    routeCard[j].Bus115 = Convert.ToString(vechileCapacityByTypes[3]);
                    routeCard[j].Trolleybus = Convert.ToString(vechileCapacityByTypes[4]);
                    routeCard[j].Tram = Convert.ToString(vechileCapacityByTypes[5]);

                    flowLayoutPanel1.Controls.Add(routeCard[j]);
                }
                
            }
            //MessageBox.Show(Convert.ToString(routes.Count));

            routesStreamReader.Close();
            streamReader.Close();
            workbook.Close();
            excel.Quit();
            

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
        for(int i=0; i < 60; i++) {
            bool attemptToStartInZeroDirectionDetected = false;
            bool attemptToStartInOneDirectionDetected = false;
            foreach(Vehicle vehicle in vehicles[i])
            {
                if (vehicle.IsMoving)
                    vehicle.IsMoving = false;
                else
                {
                    if (vehicle.NextGenCoord.IdStop == 0)
                    {
                    //textBox1.Text += vehicle.CurrentStop.IdLine + " " + vehicle.CurrentStop.IdStop + "\n";
                    //textBox1.Text += vehicle.NextGenCoord.IdLine + " " + vehicle.NextGenCoord.IdStop + "\n";
                        vehicle.OnRoute = false;
                        //высаживаем пассажиров
                        if (vehicle.Direction == 0)
                            vehicle.Direction = 1;
                        else
                            vehicle.Direction = 0;

                        vehicle.NextGenCoord.IdStop = vehicle.CurrentStop.IdStop;
                        ChangeNextGenCoord(vehicle, i);

                        vehicle.IsMoving = true;

                    //textBox1.Text += vehicle.NextGenCoord.IdLine + " " + vehicle.NextGenCoord.IdStop + "\n";
                    }
                    else
                    {

                        if (!vehicle.OnRoute)
                        {
                            if(!attemptToStartInZeroDirectionDetected || !attemptToStartInOneDirectionDetected)
                            {
                                if(vehicle.Direction == 0)
                                {
                                    attemptToStartInZeroDirectionDetected = true;
                                    if (afterStartZeroDirectionCounter == 0)
                                        StartDepature(vehicle, i);
                                    
                                }
                                else
                                {
                                    attemptToStartInOneDirectionDetected = true;
                                    if (afterStartOneDirectionCounter == 0)  
                                        StartDepature(vehicle, i);
                                    
                                }
                            }

                        }
                        else
                        {
                            //Cадим пассажиров
                            ChangeCurrentCoord(vehicle, i);
                            vehicle.IsMoving = true;
                        }
                    }
                }
            }
        }
        if (afterStartZeroDirectionCounter < depatureInterval)
            afterStartZeroDirectionCounter++;
        else
            afterStartZeroDirectionCounter = 0;

        if (afterStartOneDirectionCounter < depatureInterval)
            afterStartOneDirectionCounter++;
        else
            afterStartOneDirectionCounter = 0;
        
        if(highlightedRouteNumber != -1)
            SetNewHighligtedVehiclesPosition();

        }

        void StartDepature(Vehicle vehicle, int iterator)
        {
            vehicle.OnRoute = true;
            //Садим пассажиров
            ChangeCurrentCoord(vehicle, iterator);
            vehicle.IsMoving = true;
        }

        void ChangeCurrentCoord(Vehicle vehicle, int iterator)
        {
            if (vehicle.NextGenCoord.IdLine == vehicle.CurrentStop.IdLine)
            {
                
                if (vehicle.NextGenCoord.IdStop > vehicle.CurrentStop.IdStop)
                {
                    vehicle.CurrentStop.IdStop++;

                }
                else
                {
                    vehicle.CurrentStop.IdStop--;

                }      

                if(vehicle.NextGenCoord.IdStop == vehicle.CurrentStop.IdStop)
                {
                    ChangeNextGenCoord(vehicle, iterator);
                }
                
                
            }
            else
            {
                vehicle.CurrentStop.IdLine = vehicle.NextGenCoord.IdLine;
                vehicle.CurrentStop.IdStop = vehicle.NextGenCoord.IdStop;

                ChangeNextGenCoord(vehicle, iterator);
            }
        }

        void ChangeNextGenCoord(Vehicle vehicle, int iterator)
        {

            bool endSearch = false;
            StopCoords prevCoord = new StopCoords(vehicle.NextGenCoord.IdLine, 0);
            foreach (StopCoords coords in routes[iterator].NamenCoords.GeneralCoords)
            {
                if (coords.IdLine == vehicle.NextGenCoord.IdLine && coords.IdStop == vehicle.NextGenCoord.IdStop)
                {
                    //textBox1.Text += vehicle.NextGenCoord.IdLine + " " + vehicle.NextGenCoord.IdStop + "\n";

                    if (vehicle.Direction == 0)
                    {
                        endSearch = true;
                    }
                    else
                    {
                        vehicle.NextGenCoord.IdLine = prevCoord.IdLine;
                        vehicle.NextGenCoord.IdStop = prevCoord.IdStop;
                        break;
                    }
                }
                else
                {
                    if (endSearch)
                    {
                        vehicle.NextGenCoord.IdLine = coords.IdLine;
                        vehicle.NextGenCoord.IdStop = coords.IdStop;
                        break;
                    }
                    prevCoord = coords;
                }

            }
            //textBox1.Text += vehicle.NextGenCoord.IdLine + " " + vehicle.NextGenCoord.IdStop + "\n";
            if (vehicle.NextGenCoord.IdLine == vehicle.CurrentStop.IdLine && vehicle.NextGenCoord.IdStop == vehicle.CurrentStop.IdStop)
            {
                vehicle.NextGenCoord.IdStop = 0;
            }

        }

        void SetNewHighligtedVehiclesPosition()
        {
            vehiclesMarkersOverlay.Markers.Clear();
            try
            {
                gMap.Overlays.Remove(vehiclesMarkersOverlay);
            }
            catch (ArgumentOutOfRangeException)
            {
                //MessageBox.Show("Can't remove vehiclesMarkersOverlay");
            }

            int vehicleNum = 1;
            foreach (Vehicle vehicle in vehicles[highlightedRouteNumber])
            {
                PointLatLng coord;

                coord = linesCoords[vehicle.CurrentStop.IdLine][vehicle.CurrentStop.IdStop - 1];
                if (vehicle.IsMoving)
                {
                    PointLatLng secondaryCoord;
                    
                    if (vehicle.OnRoute)
                    {   
                        secondaryCoord = linesCoords[vehicle.PinnedCoords.IdLine][vehicle.PinnedCoords.IdStop];           
                    }
                    else
                    {
                        secondaryCoord = coord;
                        secondaryCoord.Lat -= 0.0016;
                        secondaryCoord.Lng -= 0.0016;
                    }
                          
                    coord.Lat += (secondaryCoord.Lat - coord.Lat) / 2;
                    coord.Lng += (secondaryCoord.Lng - coord.Lng) / 2;
                }
                else
                {
                    vehicle.PinnedCoords.IdLine = vehicle.CurrentStop.IdLine;
                    vehicle.PinnedCoords.IdStop = vehicle.CurrentStop.IdStop - 1;
                }
                  
                coord.Lat += 0.0004;
                coord.Lng += 0.0004;

                Bitmap iconDrawable;
                if (vehicle.Direction == 0)
                    iconDrawable = new Bitmap(Image.FromFile(@"assets/vehicleIcon0.png"));
                else
                    iconDrawable = new Bitmap(Image.FromFile(@"assets/vehicleIcon1.png"));

                GMapMarker marker = new GMarkerGoogle(coord, iconDrawable);

                string vehState;

                if (vehicle.IsMoving)
                    vehState = "в пути";
                else
                    vehState = "на остановке";

                marker.ToolTipText = "\nМаршрут " + vehicle.RouteName +
                                     "\n№ ТС на маршруте " + vehicleNum +
                                     "\nЗаполненность ТС " + vehicle.Passengers.Count + "/" + Convert.ToString(vehicle.Capacity) +
                                     "\nСтатус " + vehState;

                vehiclesMarkersOverlay.Markers.Add(marker);
                vehicleNum++;

            }
            gMap.Overlays.Add(vehiclesMarkersOverlay);
        }

       private void Form1_Load(object sender, EventArgs e)
        {
            double P;
            List<double> allLamdas = new List<double>
            {
                0.14,
                0.12
            };

            for(int z=0; z< allLamdas.Count; z++)
            {
                int summCount = 0;
                List<int> count = new List<int>();
                interval.Add(new List<int>());

                double L = Math.Exp(-1 * allLamdas[z]); 

                int factorial = 1;

                for (int i = 0; ; i++)
                {
                    P = (L * Math.Pow(allLamdas[z], i)) / factorial;

                    count.Add(Convert.ToInt32(Math.Round(timeInterval * P)));

                    if (count[i] == 0)
                        break;

                    summCount += count[i];

                    if (i > 0)
                        interval[z].Add(interval[z][i - 1] + count[i]);
                    else
                        interval[z].Add(count[i]);

                    textBox1.Text += P+" "+count[i]+" "+interval[z][i]+"\r\n";
                    factorial *= (i + 1);
                }
                textBox1.Text += summCount + "\r\nПоправка: ";


                if (summCount < timeInterval)
                {
                    count[1] += (timeInterval - summCount);

                    textBox1.Text += count[1];
                    for (int j = 1; j < interval.Capacity; j++)
                    {
                        interval[z][j] += (timeInterval - summCount);
                        textBox1.Text += interval[z][j] + "\r\n";
                    }
                    summCount += (timeInterval - summCount);
                    textBox1.Text += summCount + "\r\nСледующий: ";
                }
                else
                {
                    if (summCount > timeInterval)
                    {
                        count[1] -= (summCount - timeInterval);
                        textBox1.Text += count[1];

                        for (int j = 1; j < interval.Capacity; j++)
                        {
                            interval[z][j] -= (summCount - timeInterval);
                            textBox1.Text += interval[z][j] + "\r\n";
                        }
                        summCount -= (summCount - timeInterval);
                        textBox1.Text += summCount + "\r\nСледующий: ";
                    }
                }
            }

            

            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Start();
            //timer2.Start();
            timer3.Start();
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
         
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton2.Checked == true)
            {
                comboBox2.Enabled = false;
                comboBox1.Items.Remove("Все");
                comboBox1.Items.Remove("Конкретный маршрут");
            }
            else
            {
                comboBox2.Enabled = true;
                comboBox1.Items.Add("Все");
                comboBox1.Items.Add("Конкретный маршрут");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.SelectedItem = " ";
            highlightedRouteNumber = -1;
            selectedRouteOverlay.Routes.Clear();
            vehiclesMarkersOverlay.Markers.Clear();
            
            switch (comboBox1.SelectedItem){
                case "Все": loadAllCards(0); break;
                case "Самые свободные": loadTheMostHighlightedCards(false); break;
                case "Самые загруженные": loadTheMostHighlightedCards(true); break;
                default: flowLayoutPanel1.Controls.Clear();break;
            }
        }


        void loadAllCards(uint nextPage)
        {
            if (currentPage != nextPage || btnLeft.Enabled == false)
            {
                flowLayoutPanel1.Controls.Clear();
                comboBox2.Items.Clear();
                btnLeft.Enabled = true;
                btnRight.Enabled = true;

                RouteCard[] routeCard = new RouteCard[20];
                int firstCard;

                switch (nextPage)
                {
                    case 0: firstCard = 0; break;
                    case 1: firstCard = 20; break;
                    default: firstCard = 40; break;
                }

                label2.Text = "с " + (firstCard + 1) + " по " + (firstCard + 20);

                for(int i=firstCard; i<firstCard+20; i++)
                {
                    string routeName = routes[i].NamenCoords.RouteName;
                    string routeExtName = routes[i].NamenCoords.RouteLineExtendedName;
                    List<uint> vechileCapacity = routes[i].VehicleCapacity;

                    comboBox2.Items.Add(routeName);

                    routeCard[i - firstCard] = new RouteCard();
                    routeCard[i - firstCard].RouteName = routeName + routeExtName;
                    routeCard[i - firstCard].Bus18 = Convert.ToString(vechileCapacity[0]);
                    routeCard[i - firstCard].Bus36 = Convert.ToString(vechileCapacity[1]);
                    routeCard[i - firstCard].Bus60 = Convert.ToString(vechileCapacity[2]);
                    routeCard[i - firstCard].Bus115 = Convert.ToString(vechileCapacity[3]);
                    routeCard[i - firstCard].Trolleybus = Convert.ToString(vechileCapacity[4]);
                    routeCard[i - firstCard].Tram = Convert.ToString(vechileCapacity[5]);

                    flowLayoutPanel1.Controls.Add(routeCard[i - firstCard]);
                }
                currentPage = nextPage;
            }
        }

        void loadTheMostHighlightedCards(bool theBusiestOnes)
        {
            MessageBox.Show("ReLoadtoHighlightCards");//:TODO
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(currentPage < 2)
                loadAllCards(currentPage + 1);
            
            else
                loadAllCards(0);
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (currentPage != 0 && currentPage < 3)
                loadAllCards(currentPage - 1);

            else
                loadAllCards(2);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(Convert.ToString(comboBox1.SelectedItem) != "Конкретный маршрут")
                comboBox1.SelectedItem = "Конкретный маршрут";
            else
                flowLayoutPanel1.Controls.Clear();

            btnLeft.Enabled = false;
            btnRight.Enabled = false;

            RouteCard routeCard = new RouteCard();
            int rutIterator = 0;

            foreach(Routes rut in routes)
            {
                if(rut.NamenCoords.RouteName == Convert.ToString(comboBox2.SelectedItem))
                {
                    List<PointLatLng> highlightedRoute = new List<PointLatLng>();
                    List<StopCoords> genCoords = rut.NamenCoords.GeneralCoords;
                    StopCoords previousGenCoord = new StopCoords(0, 0);
                    foreach(StopCoords genCoord in genCoords)
                    {
                        if(previousGenCoord != new StopCoords(0, 0) && previousGenCoord.IdLine == genCoord.IdLine)
                        {
                            if(genCoord.IdStop > previousGenCoord.IdStop)
                            {
                                for(int i = previousGenCoord.IdStop - 1; i < genCoord.IdStop - 2; i++)
                                {
                                    highlightedRoute.Add(linesCoords[genCoord.IdLine][i + 1]);
                                }
                            }
                            else
                            {
                                for(int i = previousGenCoord.IdStop - 1; i > genCoord.IdStop; i--)
                                {
                                    highlightedRoute.Add(linesCoords[genCoord.IdLine][i - 1]);
                                }
                            }
                        }
                        highlightedRoute.Add(linesCoords[genCoord.IdLine][genCoord.IdStop-1]);
                        previousGenCoord.IdLine = genCoord.IdLine;
                        previousGenCoord.IdStop = genCoord.IdStop;
                    }

                    GMapRoute lineRoute = new GMapRoute(highlightedRoute, "highlightedRoute");
                    lineRoute.Stroke = new Pen(Color.BlueViolet, 6);
                    lineRoute.IsVisible = true;
                    selectedRouteOverlay.Routes.Clear();
                    try
                    {
                        gMap.Overlays.Remove(selectedRouteOverlay);
                        
                    }
                    catch (ArgumentOutOfRangeException)
                    {

                    }
                    selectedRouteOverlay.Routes.Add(lineRoute);
                    gMap.Overlays.Add(selectedRouteOverlay);

                    highlightedRouteNumber = rutIterator;
                    SetNewHighligtedVehiclesPosition();

                    routeCard.RouteName = rut.NamenCoords.RouteName + rut.NamenCoords.RouteLineExtendedName;
                    routeCard.Bus18 = Convert.ToString(rut.VehicleCapacity[0]);
                    routeCard.Bus36 = Convert.ToString(rut.VehicleCapacity[1]);
                    routeCard.Bus60 = Convert.ToString(rut.VehicleCapacity[2]);
                    routeCard.Bus115 = Convert.ToString(rut.VehicleCapacity[3]);
                    routeCard.Trolleybus = Convert.ToString(rut.VehicleCapacity[4]);
                    routeCard.Tram = Convert.ToString(rut.VehicleCapacity[5]);

                    flowLayoutPanel1.Controls.Add(routeCard);
                    break;
                }
                rutIterator++;
            }
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            timer3.Stop();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            int currRand; 
            //foreach(List<Stop> stop in stops)
            //{
                foreach(Stop st in stops[34])
                {
                    currRand = rand.Next(1, timeInterval);

                    textBox1.Text = Convert.ToString(currRand);
                    for(int i=0; i< interval[0].Count; i++)
                    {
                        if (currRand <= interval[0][i])
                        {
                            for (int j = 0; j < i; j++)
                            {
                                int chosenLine = rand.Next(0, 42);
                                int chosenStop = rand.Next(1, stops[chosenLine].Count);
                                st.Passengers.Add(new Passengers(new StopCoords(chosenLine, chosenStop), Convert.ToInt32(systemTime)));
                            }
                        }
                    }
                    
                //}

            }

        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            label5.Text = systemTime + " сек.";

            //вызов функции перерисования маркеров остановок
            try
            {
                gMap.Overlays.Remove(stopMarkersOverlay);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Can't remove stopMarkersOverlay");
            }

            foreach (List<Stop> stop in stops)
            {
                foreach(Stop st in stop)
                {
                    GMapMarker marker = new GMarkerGoogle(
                        st.StopCoords,
                        new Bitmap(Image.FromFile(@"assets/stopGreenIcon.png"))
                        );
                    marker.ToolTipText = "\n" + st.StopName + "\nКоличество пассажиров: " + st.Passengers.Count;

                    stopMarkersOverlay.Markers.Add(marker);

                }
                
            }

            gMap.Overlays.Add(stopMarkersOverlay);

            systemTime++;
        }
    }

    public class Vehicle
    {
        public Vehicle(String routeName,StopCoords nextGenCoord, StopCoords currentStop, StopCoords pinnedCoords, uint direction, Boolean onRoute, Boolean isMoving,int capacity, List<Passengers> passengers)
        {
           
            RouteName = routeName;
            NextGenCoord = nextGenCoord;
            CurrentStop = currentStop;
            PinnedCoords = pinnedCoords;
            Direction = direction;
            OnRoute = onRoute;
            IsMoving = isMoving;
            Capacity = capacity;
            Passengers = passengers;
        }
        public String RouteName { get; set; }
        public StopCoords NextGenCoord { get; set; }
        public StopCoords CurrentStop { get; set; }
        public StopCoords PinnedCoords { get; set; }
        public uint Direction { get; set; }
        public Boolean OnRoute { get; set; }
        public Boolean IsMoving { get; set; }
        public int Capacity { get; set; }

        public List<Passengers> Passengers { get; set; }

    }

    public class Passengers
    {
        public Passengers(StopCoords destination, int spawnTime)
        {
            Destination = destination;
            SpawnTime = spawnTime;
        }

        public StopCoords Destination { get; set; }
        public int SpawnTime { get; set; }
    }

    public class Stop
    {
        public Stop(List<Passengers> passengers, String stopName, PointLatLng stopCoords)
        {
            Passengers = passengers;
            StopName = stopName;
            StopCoords = stopCoords;

        }
        public List<Passengers> Passengers { get; set; }
        public String StopName { get; set; }
        public PointLatLng StopCoords { get; set; }
    }

    public class Routes
    {
        public Routes(RouteInfo routeInfo, List<uint> vehicleCapacity)
        {
            NamenCoords = routeInfo;
            VehicleCapacity = vehicleCapacity;
        }
        public RouteInfo NamenCoords { get; set; }
        public List<uint> VehicleCapacity { get; set; }
    }

    public class RouteInfo
    {
        public RouteInfo(String routeName, String routeLineExtendedName, List<StopCoords> generalCoords)
        {
            RouteName = routeName;
            RouteLineExtendedName = routeLineExtendedName;
            GeneralCoords = generalCoords;
        }
        public String RouteName { get; set; }
        public String RouteLineExtendedName { get; set; }
        public List<StopCoords> GeneralCoords { get; set; }
    }

    public class StopCoords
    {
        public StopCoords(int idLine, int idStop)
        {
            IdLine = idLine;
            IdStop = idStop;
        }
        public int IdLine { get; set; }
        public int IdStop { get; set; }
    }

}
