﻿using CustomDrawCrosshairSample.Model;
using DevExpress.Drawing;
using DevExpress.XtraCharts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CustomDrawCrosshairSample {
    public partial class Form1 : DevExpress.XtraEditors.XtraForm {
        Dictionary<string, DXImage> photoCache = new Dictionary<string, DXImage>();
        Dictionary<string, DXBitmap> bitmapCache = new Dictionary<string, DXBitmap>();
        #region #Constants
        const int borderSize = 2;
        const int scaledPhotoWidth = 32;
        const int scaledPhotoHeight = 34;
        // Width and height of scaled photo with border.
        const int totalWidth = 36;
        const int totalHeight = 38;

        // Rects required to create a custom legend series marker.
        static readonly Rectangle photoRect = new Rectangle(
                borderSize, borderSize,
                scaledPhotoWidth, scaledPhotoHeight);
        static readonly Rectangle totalRect = new Rectangle(
                0, 0,
                totalWidth, totalHeight);
        #endregion

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            using (var context = new NwindDbContext()) {
                InitPhotoCache(context.Employees);
                chart.DataSource = context.Orders.ToList();
            }

            chart.SeriesDataMember = "Employee.FullName";
            chart.SeriesTemplate.ArgumentDataMember = "OrderDate";
            chart.SeriesTemplate.ValueDataMembers.AddRange("Freight");

            XYDiagram diagram = chart.Diagram as XYDiagram;
            if (diagram != null) {
                diagram.AxisX.DateTimeScaleOptions.AggregateFunction = AggregateFunction.Sum;
                diagram.AxisX.DateTimeScaleOptions.MeasureUnit = DateTimeMeasureUnit.Year;
            }

            chart.CustomDrawCrosshair += OnCustomDrawCrosshair;
        }

        void InitPhotoCache(IEnumerable<Employee> employees) {
            photoCache.Clear();
            foreach (var employee in employees) {
                using (MemoryStream stream = new MemoryStream(employee.Photo)) {
                    if (!photoCache.ContainsKey(employee.FullName))
                        photoCache.Add(employee.FullName, DXImage.FromStream(stream));
                }
            }
        }

        #region #CustomDrawCrosshairHandler
        private void OnCustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e) {
            foreach (CrosshairElementGroup group in e.CrosshairElementGroups) {
                if (group.CrosshairElements[0] != null)
                    group.HeaderElement.Text = String.Format("Sales in {0:yyyy}", group.CrosshairElements[0].SeriesPoint.DateTimeArgument);
                foreach (CrosshairElement element in group.CrosshairElements) {
                    DXBitmap image;
                    if (!bitmapCache.TryGetValue(element.Series.Name, out image)) {
                        image = new DXBitmap(totalWidth, totalHeight);
                        using (DXGraphics graphics = DXGraphics.FromImage(image)) {
                            using (DXSolidBrush brush = new DXSolidBrush(element.LabelElement.MarkerColor)) {
                                graphics.FillRectangle(brush, totalRect);
                            }
                            DXImage photo;
                            if (photoCache.TryGetValue(element.Series.Name, out photo))
                                graphics.DrawImage(photo, photoRect);
                        }
                        bitmapCache.Add(element.Series.Name, image);
                    }
                    element.LabelElement.DXMarkerImage = image;
                    element.LabelElement.MarkerSize = new Size(totalWidth, totalHeight);
                }
            }
        }
        #endregion 
    }
}
