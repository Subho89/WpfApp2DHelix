using HelixToolkit.Wpf;
using System.Globalization;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp2DHelix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModelVisual3D pointVisual;
        private SphereVisual3D pointSphere;

        private bool isDragging = false;
        private Point lastMousePos;

        private double minX = -10, maxX = 10;
        private double minY = -10, maxY = 10;
        private double minZ = -10, maxZ = 10;

        private BillboardTextVisual3D coordLabel;

        // Keep references to axis labels for font size updates
        private BillboardTextVisual3D xLabel, yLabel, zLabel;

        private bool isPerspective = true;
        private bool isUpdatingUI = false;

        private bool txtUpdating = false;
        private bool sliderUpdating = false;
        RangeVM rangeX = new RangeVM();
        RangeVM rangeY = new RangeVM();
        RangeVM rangeZ = new RangeVM();
        public int decimalX;
        public int decimalY;
        public int decimalZ;
        public int expressionX;
        public int expressionY;
        public int expressionZ;

        public MainWindow()
        {

            InitializeComponent();
            DrawGrid();
            AddPoint(0, 0, 0);

            var cam = view1.Camera as PerspectiveCamera;
            if (cam != null)
            {
                cam.Position = new Point3D(0, 0, 86.71035032622612);      // now at +X +Y +Z corner
                cam.LookDirection = new Vector3D(-0, -0, -86.71035032622612); // looking back at origin
                cam.UpDirection = new Vector3D(0, 1, 0);
            }

            txtX.LostFocus += TxtPointerInput_LostFocus;
            txtY.LostFocus += TxtPointerInput_LostFocus;
            txtX.TextChanged += txtPointerInput_TextChanged;
            txtY.TextChanged += txtPointerInput_TextChanged;
            txtMinX.TextChanged += txtRange_TextChanged;
            txtMinY.TextChanged += txtRange_TextChanged;
            txtMaxX.TextChanged += txtRange_TextChanged;
            txtMaxY.TextChanged += txtRange_TextChanged;
            sliderPointSize.ValueChanged += sliderPointSize_ValueChanged;
            sliderFontSize.ValueChanged += sliderFontSize_ValueChanged;
            txtFontMin.TextChanged += txtFont_TextChanged;
            txtFontMax.TextChanged += txtFont_TextChanged;
            txtPointMin.TextChanged += txtPoint_TextChanged;
            txtPointMax.TextChanged += txtPoint_TextChanged;
        }


        private List<BillboardTextVisual3D> xTicks = new();
        private List<BillboardTextVisual3D> yTicks = new();
        private List<BillboardTextVisual3D> zTicks = new();

        private void DrawGrid()
        {
            view1.Children.Clear();
            view1.Children.Add(new DefaultLights());

            // Parse step values (fallback to 1 if invalid)
            double stepX = 1, stepY = 1;
            double.TryParse(txtStepX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepX);
            double.TryParse(txtStepY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepY);

            // reset tick references
            xTicks.Clear();
            yTicks.Clear();

            // ---------------- AXIS ORIGIN ----------------
            double originX = (minX <= 0 && 0 <= maxX) ? 0 : minX;
            double originY = (minY <= 0 && 0 <= maxY) ? 0 : minY;
            double originZ = 0; // fixed at 0 for 2D

            // ---------------- GRID ----------------
            var gridXY = new LinesVisual3D { Color = Colors.LightGray, Thickness = 0.5 };
            int stepsY = (int)Math.Round((maxY - minY) / stepY);
            int stepsX = (int)Math.Round((maxX - minX) / stepX);

            // Horizontal lines
            for (int i = 0; i <= stepsY; i++)
            {
                double y = Math.Round(minY + i * stepY, 6);
                gridXY.Points.Add(new Point3D(minX, y, originZ));
                gridXY.Points.Add(new Point3D(maxX, y, originZ));
            }

            // Vertical lines
            for (int i = 0; i <= stepsX; i++)
            {
                double x = Math.Round(minX + i * stepX, 6);
                gridXY.Points.Add(new Point3D(x, minY, originZ));
                gridXY.Points.Add(new Point3D(x, maxY, originZ));
            }
            view1.Children.Add(gridXY);

            // ---------------- AXES ----------------
            view1.Children.Add(new LinesVisual3D
            {
                Color = Colors.Red,
                Thickness = 2,
                Points = new Point3DCollection { new Point3D(minX, originY, originZ), new Point3D(maxX, originY, originZ) }
            });
            view1.Children.Add(new LinesVisual3D
            {
                Color = Colors.Green,
                Thickness = 2,
                Points = new Point3DCollection { new Point3D(originX, minY, originZ), new Point3D(originX, maxY, originZ) }
            });

            // ---------------- AXIS LABELS ----------------
            xLabel = new BillboardTextVisual3D
            {
                Text = "X",
                Position = new Point3D(maxX + 0.5, originY, originZ),
                Foreground = Brushes.Red,
                FontSize = sliderFontSize.Value,
                FontWeight = FontWeights.Bold
            };
            yLabel = new BillboardTextVisual3D
            {
                Text = "Y",
                Position = new Point3D(originX, maxY + 0.5, originZ),
                Foreground = Brushes.Green,
                FontSize = sliderFontSize.Value,
                FontWeight = FontWeights.Bold
            };
            view1.Children.Add(xLabel);
            view1.Children.Add(yLabel);

            // ---------------- RULER TICKS ----------------
            for (int i = 0; i <= stepsX; i++)
            {
                double x = Math.Round(minX + i * stepX, 6);
                view1.Children.Add(new LinesVisual3D
                {
                    Color = Colors.Red,
                    Points = new Point3DCollection
            {
                new Point3D(x, originY - 0.1, originZ),
                new Point3D(x, originY + 0.1, originZ)
            }
                });
                var tick = new BillboardTextVisual3D
                {
                    Text = x.ToString("0.###"),
                    Position = new Point3D(x, originY - 0.3, originZ),
                    Foreground = Brushes.Red,
                    FontSize = sliderFontSize.Value
                };
                view1.Children.Add(tick); xTicks.Add(tick);
            }

            for (int i = 0; i <= stepsY; i++)
            {
                double y = Math.Round(minY + i * stepY, 6);
                view1.Children.Add(new LinesVisual3D
                {
                    Color = Colors.Green,
                    Points = new Point3DCollection
            {
                new Point3D(originX - 0.1, y, originZ),
                new Point3D(originX + 0.1, y, originZ)
            }
                });
                var tick = new BillboardTextVisual3D
                {
                    Text = y.ToString("0.###"),
                    Position = new Point3D(originX - 0.3, y, originZ),
                    Foreground = Brushes.Green,
                    FontSize = sliderFontSize.Value
                };
                view1.Children.Add(tick); yTicks.Add(tick);
            }

            // ---------------- RE-ADD POINTER + LABEL ----------------
            if (pointSphere != null) view1.Children.Add(pointSphere);
            if (coordLabel != null) view1.Children.Add(coordLabel);
            if (pointSphere != null) DrawGuides(pointSphere.Center);


            AdjustCameraToGrid();
        }


        private void DrawGuides(Point3D p)
        {
            // remove old guide lines
            for (int i = view1.Children.Count - 1; i >= 0; i--)
            {
                if (view1.Children[i] is LinesVisual3D line &&
                    (line.Color == Colors.Goldenrod))
                {
                    view1.Children.RemoveAt(i);
                }
            }

            var guideThickness = 2.0;

            // Use origin as 0 if available in all ranges, else min values
            double originX = (minX <= 0 && maxX >= 0) ? 0 : minX;
            double originY = (minY <= 0 && maxY >= 0) ? 0 : minY;
            double originZ = 0; // fixed at 0 for 2D

            // === Yellow Guides (2D only, XY plane) ===
            // X → Y guide
            view1.Children.Add(new LinesVisual3D
            {
                Color = Colors.Goldenrod,
                Thickness = guideThickness,
                Points = new Point3DCollection
                {
                    new Point3D(p.X, originY, originZ),
                    new Point3D(p.X, p.Y, originZ)
                }
            });

            // Y → X guide
            view1.Children.Add(new LinesVisual3D
            {
                Color = Colors.Goldenrod,
                Thickness = guideThickness,
                Points = new Point3DCollection
                {
                    new Point3D(originX, p.Y, originZ),
                    new Point3D(p.X, p.Y, originZ)
                }
            });
        }



        private void AddPoint(double x, double y, double z)
        {
            // Create the sphere once
            var sphere = new SphereVisual3D
            {
                Center = new Point3D(x, y, z),
                Radius = 0.2,
                Fill = Brushes.Red
            };

            pointSphere = sphere;
            view1.Children.Add(pointSphere); // only here
        }

        private void UpdateSphereLabel(Point3D p)
        {
            // remove old label if exists
            if (coordLabel != null)
                view1.Children.Remove(coordLabel);

            // format each coordinate individually
            string formatX = decimalX == 0 ? "0" : "F" + decimalX;
            string formatY = decimalY == 0 ? "0" : "F" + decimalY;

            string text = $"({p.X.ToString(formatX, CultureInfo.InvariantCulture)}, " +
                          $"{p.Y.ToString(formatY, CultureInfo.InvariantCulture)} )";

            // create label
            coordLabel = new BillboardTextVisual3D
            {
                Text = text,
                Position = new Point3D(p.X + 2, p.Y + 2, 0),
                Foreground = Brushes.DarkBlue,
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };

            view1.Children.Add(coordLabel);
        }


        private void UpdatePoint(double x, double y, double z = 0)
        {
            if (pointSphere != null)
            {
                pointSphere.Center = new Point3D(x, y, z);
                DrawGuides(new Point3D(x, y, z));

                isUpdatingUI = true; // prevent recursive TextChanged

                // X formatting
                if (decimalX == 0)
                    txtX.Text = x.ToString("0", CultureInfo.InvariantCulture);
                else
                    txtX.Text = x.ToString("F" + decimalX, CultureInfo.InvariantCulture);
                txtX.CaretIndex = txtX.Text.Length;

                // Y formatting
                if (decimalY == 0)
                    txtY.Text = y.ToString("0", CultureInfo.InvariantCulture);
                else
                    txtY.Text = y.ToString("F" + decimalY, CultureInfo.InvariantCulture);
                txtY.CaretIndex = txtY.Text.Length;



                isUpdatingUI = false;
            }
        }



        private void view1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && pointSphere != null)
            {
                Point pos = e.GetPosition(view1);

                var current = pointSphere.Center;
                double newX = current.X;
                double newY = current.Y;
                double newZ = current.Z;

                //Control XY
                double dx = (pos.X - lastMousePos.X) * (maxX - minX) / 500;
                double dy = -(pos.Y - lastMousePos.Y) * (maxY - minY) / 500;

                newX = Math.Max(minX, Math.Min(maxX, current.X + dx));
                newY = Math.Max(minY, Math.Min(maxY, current.Y + dy));

                //  Snap-to-Grid logic with separate steps
                double stepX = Convert.ToDouble(txtStepX.Text), stepY = Convert.ToDouble(txtStepY.Text);
                double.TryParse(txtStepX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepX);
                double.TryParse(txtStepY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepY);

                if (chkSnapToGridX.IsChecked == true)
                {
                    newX = GetSnappedValue(newX, stepX);
                    //newY = GetSnappedValue(newY, stepY);
                    //newZ = GetSnappedValue(newZ, stepZ);
                }

                if (chkSnapToGridY.IsChecked == true)
                {
                    // newX = GetSnappedValue(newX, stepX);
                    newY = GetSnappedValue(newY, stepY);
                    //newZ = GetSnappedValue(newZ, stepZ);
                }

                // Apply updates
                UpdatePoint(newX, newY, newZ);
                UpdateSphereLabel(new Point3D(newX, newY, newZ));

                txtX.Text = newX.ToString("F2");
                txtY.Text = newY.ToString("F2");

                // Update sliders too
                sliderX.Value = newX;
                sliderY.Value = newY;

                lastMousePos = pos;
            }
        }



        private void btnSetStep_Click(object sender, RoutedEventArgs e)
        {


        }

        private void view1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (pointSphere != null)
            {
                var current = pointSphere.Center;

                // Move Z in steps, scaled by delta
                double dz = (e.Delta > 0 ? 0.2 : -0.2) * (maxZ - minZ) / 20;
                double newZ = Math.Max(minZ, Math.Min(maxZ, current.Z + dz));

                UpdatePoint(current.X, current.Y, newZ);

                txtX.Text = current.X.ToString("F2");
                txtY.Text = current.Y.ToString("F2");
            }
        }

        private void view1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (pointSphere != null)
            {
                isDragging = true;
                lastMousePos = e.GetPosition(view1);
                view1.CaptureMouse(); // lock mouse to viewport
            }
        }

        private void view1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                view1.ReleaseMouseCapture();

                if (pointSphere != null)
                {
                    double stepX = 1, stepY = 1, stepZ = 1;
                    double.TryParse(txtStepX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepX);
                    double.TryParse(txtStepY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out stepY);

                    double newX = pointSphere.Center.X;
                    double newY = pointSphere.Center.Y;
                    double newZ = pointSphere.Center.Z;

                    if (chkSnapToGridX.IsChecked == true)
                        newX = GetSnappedValue(newX, stepX);

                    if (chkSnapToGridY.IsChecked == true)
                        newY = GetSnappedValue(newY, stepY);

                    UpdatePoint(newX, newY, newZ);
                    UpdateSphereLabel(new Point3D(newX, newY, newZ));

                    txtX.Text = newX.ToString("F2", CultureInfo.InvariantCulture);
                    txtY.Text = newY.ToString("F2", CultureInfo.InvariantCulture);

                    sliderX.Value = newX;
                    sliderY.Value = newY;
                }
            }
        }
        private void TxtPointerInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                    tb.Text = "0";

                // Parse and clamp value
                double value = Clamp(ParseOrDefault(tb.Text),
                                     tb == txtX ? minX : tb == txtY ? minY : minZ,
                                     tb == txtX ? maxX : tb == txtY ? maxY : maxZ);

                // Format to fixed decimals
                int decimals = tb == txtX ? decimalX : tb == txtY ? decimalY : decimalZ;
                int oldCaret = tb.CaretIndex;
                int oldLength = tb.Text.Length;

                string newText = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
                tb.Text = newText;

                // Adjust caret to stay close to where user typed
                int newLength = newText.Length;
                int delta = newLength - oldLength;
                tb.CaretIndex = Math.Max(0, oldCaret + delta);

                // ✅ Update 3D point and sphere only on focus loss
                double x = Clamp(ParseOrDefault(txtX.Text), minX, maxX);
                double y = Clamp(ParseOrDefault(txtY.Text), minY, maxY);

                UpdatePoint(x, y, 0);
                UpdateSphereLabel(new Point3D(x, y, 0));

                // Update sliders
                sliderX.Value = x;
                sliderY.Value = y;
            }
        }

        private void txtPointerInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sliderUpdating || isUpdatingUI || txtUpdating)
                return;

            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string text = tb.Text;

            // Allow intermediate typing states
            if (string.IsNullOrEmpty(text) || text == "-" || text == "." || text == "-.")
                return;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                txtUpdating = true;

                // Clamp values from each textbox
                double x = Clamp(ParseOrDefault(txtX.Text), minX, maxX);
                double y = Clamp(ParseOrDefault(txtY.Text), minY, maxY);
                double z = Clamp(ParseOrDefault("0"), minZ, maxZ);

                txtUpdating = false;
            }
        }

        private string FormatWithDecimals(double value, int decimals)
        {
            if (decimals == 0)
                return value.ToString("0", CultureInfo.InvariantCulture);
            else
                return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        // 🔹 Helpers
        private static double Clamp(double value, double min, double max) =>
            Math.Max(min, Math.Min(max, value));

        private static double ParseOrDefault(string text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private void txtRange_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (double.TryParse(txtMinX.Text, out double newMinX) &&
               double.TryParse(txtMaxX.Text, out double newMaxX) &&
               double.TryParse(txtMinY.Text, out double newMinY) &&
               double.TryParse(txtMaxY.Text, out double newMaxY) &&
               double.TryParse("0", out double newMinZ) &&
               double.TryParse("0", out double newMaxZ))
            {
                // update ranges
                minX = newMinX; maxX = newMaxX;
                minY = newMinY; maxY = newMaxY;
                minZ = newMinZ; maxZ = newMaxZ;

                // === determine origin for each axis ===
                double originX = (minX <= 0 && maxX >= 0) ? 0 : minX;
                double originY = (minY <= 0 && maxY >= 0) ? 0 : minY;
                double originZ = (minZ <= 0 && maxZ >= 0) ? 0 : minZ;

                // redraw grid with updated origin
                DrawGrid();

                // reset sliders
                sliderX.Minimum = minX; sliderX.Maximum = maxX;
                sliderY.Minimum = minY; sliderY.Maximum = maxY;
                //sliderZ.Minimum = minZ; sliderZ.Maximum = maxZ;

                // set slider values to origin
                sliderX.Value = originX;
                sliderY.Value = originY;
                //sliderZ.Value = originZ;

                // reset sphere and guides
                UpdatePoint(originX, originY, originZ);
                AddPoint(originX, originY, originZ);

                isUpdatingUI = true;
                txtX.Text = originX.ToString(CultureInfo.InvariantCulture);
                txtY.Text = originY.ToString(CultureInfo.InvariantCulture);
                txtCoordinateX.Text = originX.ToString(CultureInfo.InvariantCulture);
                txtCoordinateY.Text = originY.ToString(CultureInfo.InvariantCulture);
                isUpdatingUI = false;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtUpdating) return;

            sliderUpdating = true;

            double newX = sliderX.Value;
            double newY = sliderY.Value;

            double stepX = ParseStep(txtStepX.Text, 1);
            double stepY = ParseStep(txtStepY.Text, 1);

            if (chkSnapToGridX.IsChecked == true)
            {
                newX = SnapWithinRange(newX, sliderX.Minimum, sliderX.Maximum, stepX);
                if (Math.Abs(sliderX.Value - newX) > double.Epsilon)
                    sliderX.Value = newX;
            }

            if (chkSnapToGridY.IsChecked == true)
            {
                newY = SnapWithinRange(newY, sliderY.Minimum, sliderY.Maximum, stepY);
                if (Math.Abs(sliderY.Value - newY) > double.Epsilon)
                    sliderY.Value = newY;
            }

            // Update UI textboxes
            txtX.Text = newX.ToString(CultureInfo.InvariantCulture);
            txtY.Text = newY.ToString(CultureInfo.InvariantCulture);
            //txtZ.Text = newZ.ToString(CultureInfo.InvariantCulture);

            // Apply expressions
            ApplyExpressionToX();
            ApplyExpressionToY();

            // Final parsed values
            if (!double.TryParse(txtX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out newX)) newX = 0;
            if (!double.TryParse(txtY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out newY)) newY = 0;

            UpdatePoint(newX, newY, 0);
            UpdateSphereLabel(new Point3D(newX, newY, 0));

            sliderUpdating = false;
        }

        private double SnapWithinRange(double value, double min, double max, double step)
        {
            if (step <= 0) return value;

            // Snap to nearest multiple of step from min
            double snapped = min + Math.Round((value - min) / step) * step;

            // Clamp to [min, max]
            if (snapped < min) snapped = min;
            if (snapped > max) snapped = max;

            return snapped;
        }

        private double ParseStep(string input, double fallback)
        {
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double step) && step > 0)
                return step;
            return fallback;
        }

        private void sliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int size = Math.Clamp((int)e.NewValue, int.Parse(txtFontMin.Text), int.Parse(txtFontMax.Text));

            if (xLabel != null) xLabel.FontSize = size;
            if (yLabel != null) yLabel.FontSize = size;
            if (zLabel != null) zLabel.FontSize = size;
            if (coordLabel != null) coordLabel.FontSize = size;

            foreach (var tick in xTicks) tick.FontSize = size;
            foreach (var tick in yTicks) tick.FontSize = size;
            foreach (var tick in zTicks) tick.FontSize = size;

            //DrawGrid();
        }

        private void txtFont_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtFontMin.Text, out double min) &&
                double.TryParse(txtFontMax.Text, out double max) &&
                min < max)
            {
                sliderFontSize.Minimum = min;
                sliderFontSize.Maximum = max;

                // keep slider value within new bounds
                if (sliderFontSize.Value < min) sliderFontSize.Value = min;
                if (sliderFontSize.Value > max) sliderFontSize.Value = max;
            }
        }

        private void txtPoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtPointMin.Text, out double min) &&
               double.TryParse(txtPointMax.Text, out double max) &&
               min < max)
            {
                sliderPointSize.Minimum = min;
                sliderPointSize.Maximum = max;

                if (sliderPointSize.Value < min) sliderPointSize.Value = min;
                if (sliderPointSize.Value > max) sliderPointSize.Value = max;
            }
        }
        private void sliderPointSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double size = Math.Clamp(e.NewValue, double.Parse(txtPointMin.Text), double.Parse(txtPointMax.Text));
            if (pointSphere != null) pointSphere.Radius = size;
        }

        private double GetSnappedValue(double value, double step)
        {
            return Math.Round(value / step) * step;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Simulate new text after keystroke
            string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            // Allow optional sign, digits, and up to 6 decimals
            Regex regex = new Regex(@"^-?\d*(\.\d{0,6})?$");

            e.Handled = !regex.IsMatch(newText);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                TextBox textBox = sender as TextBox;

                string newText = textBox.Text.Insert(textBox.CaretIndex, pastedText);
                Regex regex = new Regex(@"^-?\d*(\.\d{0,6})?$");

                if (!regex.IsMatch(newText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void txtMinMax_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int value))
            {
                if (value < 5) tb.Text = "5"; // Enforce min size = 5
            }
        }

        private void BtnSetXRange_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtMinX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double min) &&
                double.TryParse(txtMaxX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double max))
            {
                if (rangeX == null)
                {
                    rangeX = new RangeVM();
                }

                rangeX.axis = "X";
                rangeX.min = (txtMinX.Text);
                rangeX.max = (txtMaxX.Text);
                rangeX.expression = txtXExp.Text;
                rangeX.step = Convert.ToDouble(txtStepX.Text);

                var dlg = new RangeDialog(rangeX) { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    // Update min/max values
                    txtMinX.Text = dlg.MinValue.ToString(CultureInfo.InvariantCulture);
                    txtMaxX.Text = dlg.MaxValue.ToString(CultureInfo.InvariantCulture);
                    sliderX.Minimum = Convert.ToDouble(dlg.MinValue);
                    sliderX.Maximum = Convert.ToDouble(dlg.MaxValue);
                    txtXExp.Text = dlg.Expression;
                    txtStepX.Text = dlg.step.ToString(CultureInfo.InvariantCulture);

                    rangeX.axis = "X";
                    rangeX.min = (txtMinX.Text);
                    rangeX.max = (txtMaxX.Text);
                    rangeX.expression = txtXExp.Text;
                    rangeX.step = Convert.ToDouble(txtStepX.Text);
                    rangeX.roundingDigits = dlg.Digits;
                    rangeX.rounding = dlg.rounding;
                    rangeX.expressionDigits = dlg.expressionDigit;

                    if (rangeX.rounding == 0)
                    {
                        decimalX = rangeX.roundingDigits;
                    }
                    else if (rangeX.rounding == 2 || rangeX.rounding == 3)
                    {
                        chkSnapToGridX.IsChecked = true;
                        decimalX = 0;
                    }
                    else
                    {
                        decimalX = 0;
                    }

                    expressionX = rangeX.expressionDigits;


                    DrawGrid();
                    // Save rounding / digits / expression
                    ApplyRangeSettings("X", dlg);

                    // Get current coordinates
                    double x = pointSphere.Center.X;
                    double y = pointSphere.Center.Y;
                    double z = pointSphere.Center.Z;


                    // Apply the expression immediately
                    ApplyExpressionToX();
                    ApplyExpressionToY();
                    x = Convert.ToDouble(txtX.Text);
                    //x = Convert.ToDouble(txtCoordinateX.Text);
                    // Update the pointer
                    UpdatePoint(x, y, z);
                    UpdateSphereLabel(new Point3D(x, y, 0));
                    //sliderX.Value=Convert.ToDouble(rangeX.min);
                }
            }
        }

        private void BtnSetYRange_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtMinY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double min) &&
                double.TryParse(txtMaxY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double max))
            {
                if (rangeY == null)
                {
                    rangeY = new RangeVM();
                }

                rangeY.axis = "Y";
                rangeY.min = (txtMinY.Text);
                rangeY.max = (txtMaxY.Text);
                rangeY.expression = txtYExp.Text;
                rangeY.step = Convert.ToDouble(txtStepY.Text);


                var dlg = new RangeDialog(rangeY) { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    // Update min/max values
                    txtMinY.Text = dlg.MinValue.ToString(CultureInfo.InvariantCulture);
                    txtMaxY.Text = dlg.MaxValue.ToString(CultureInfo.InvariantCulture);
                    sliderY.Minimum = Convert.ToDouble(dlg.MinValue);
                    sliderY.Maximum = Convert.ToDouble(dlg.MaxValue);
                    txtYExp.Text = dlg.Expression;
                    txtStepY.Text = dlg.step.ToString(CultureInfo.InvariantCulture);

                    rangeY.axis = "Y";
                    rangeY.min = (txtMinY.Text);
                    rangeY.max = (txtMaxY.Text);
                    rangeY.expression = txtYExp.Text;
                    rangeY.step = Convert.ToDouble(txtStepY.Text);
                    rangeY.roundingDigits = dlg.Digits;
                    rangeY.rounding = dlg.rounding;
                    rangeY.expressionDigits = dlg.expressionDigit;

                    if (rangeY.rounding == 0)
                    {
                        decimalY = rangeY.roundingDigits;
                    }
                    else if (rangeY.rounding == 2 || rangeY.rounding == 3)
                    {
                        chkSnapToGridY.IsChecked = true;
                        decimalX = 0;
                    }
                    else
                    {
                        decimalY = 0;
                    }

                    expressionY = rangeY.expressionDigits;

                    DrawGrid();
                    // Save rounding / digits / expression
                    ApplyRangeSettings("Y", dlg);

                    // Get current coordinates
                    double x = pointSphere.Center.X;
                    double y = pointSphere.Center.Y;
                    double z = pointSphere.Center.Z;


                    // Apply the expression immediately
                    ApplyExpressionToX();
                    ApplyExpressionToY();
                    y = Convert.ToDouble(txtY.Text);
                    //y = Convert.ToDouble(txtCoordinateY.Text);
                    // Update the pointer
                    UpdatePoint(x, y, z);
                    UpdateSphereLabel(new Point3D(x, y, 0));
                }
            }
        }

        /// <summary>
        /// Store additional settings returned from RangeDialog
        /// </summary>
        private void ApplyRangeSettings(string axis, RangeDialog dlg)
        {
            // Example: You could store them in dictionaries if you want per-axis configs
            // For now, just showing how to capture them
            var rounding = dlg.SelectedMode;
            var digits = dlg.Digits;
            var expr = dlg.Expression;

            // Debug or apply logic later
            Console.WriteLine($"Axis={axis}, Mode={rounding}, Digits={digits}, Expression={expr}");
        }

        private void ApplyExpressionToX()
        {
            if (double.TryParse(txtX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double originalX))
            {
                string expression = txtXExp.Text;

                if (!string.IsNullOrWhiteSpace(expression))
                {
                    try
                    {
                        var exp = new NCalc.Expression(expression);
                        exp.Parameters["X"] = originalX;
                        exp.Parameters["Y"] = pointSphere.Center.Y;
                        exp.Parameters["Z"] = pointSphere.Center.Z;

                        var result = exp.Evaluate();

                        double value;
                        if (result is double d)
                            value = d;
                        else if (!double.TryParse(result.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                            value = originalX;

                        // Get the number of decimal places from expressionX
                        int decimals = (int)expressionX; // expressionX = 0, 1, 2, ...

                        // Format exactly with decimals
                        txtCoordinateX.Text = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        txtCoordinateX.Text = "ERR";
                        Console.WriteLine($"Expression error in X: {ex.Message}");
                    }
                }
                else
                {
                    // No expression: still respect decimals
                    int decimals = (int)expressionX;
                    txtCoordinateX.Text = originalX.ToString("F" + decimals, CultureInfo.InvariantCulture);
                }
            }
        }


        private void ApplyExpressionToY()
        {
            if (double.TryParse(txtY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double originalY))
            {
                string expression = txtYExp.Text;

                double value = originalY; // fallback value

                if (!string.IsNullOrWhiteSpace(expression))
                {
                    try
                    {
                        var exp = new NCalc.Expression(expression);
                        exp.Parameters["X"] = pointSphere.Center.X;
                        exp.Parameters["Y"] = originalY;
                        exp.Parameters["Z"] = pointSphere.Center.Z;

                        var result = exp.Evaluate();

                        if (result is double d)
                            value = d;
                        else if (!double.TryParse(result.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                            value = originalY;
                    }
                    catch (Exception ex)
                    {
                        txtCoordinateY.Text = "ERR";
                        Console.WriteLine($"Expression error in Y: {ex.Message}");
                        return;
                    }
                }

                // Respect decimal count from expressionY
                int decimals = (int)expressionY; // expressionY = 0, 1, 2, ...
                txtCoordinateY.Text = value.ToString("F" + decimals, CultureInfo.InvariantCulture);
            }
        }


        private void AdjustCameraToGrid()
        {
            // Compute center of current grid
            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double centerZ = 0;

            // Estimate grid "size"
            double sizeX = Math.Abs(maxX - minX);
            double sizeY = Math.Abs(maxY - minY);
            double size = Math.Max(sizeX, sizeY);

            // Get your existing camera
            var cam = view1.Camera as PerspectiveCamera;
            if (cam == null) return;

            // Place camera at an appropriate distance so the grid fits well
            // The multiplier 2.0–3.0 decides how zoomed out the view is
            double distance = size * 2.0;

            // Reset camera position and look direction (static 2D view)
            cam.Position = new Point3D(centerX, centerY, distance);
            cam.LookDirection = new Vector3D(0, 0, -1);  // looking straight down Z-axis
            cam.UpDirection = new Vector3D(0, 1, 0);     // Y up
        }

    }
}