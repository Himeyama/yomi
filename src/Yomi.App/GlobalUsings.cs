// UseWindowsForms=true により System.Drawing / System.Windows.Forms の同名型と
// 暗黙的にあいまいになるため、WPF (System.Windows.*) 側を既定として解決する。
global using Application = System.Windows.Application;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Color = System.Windows.Media.Color;
global using Pen = System.Windows.Media.Pen;
global using Point = System.Windows.Point;
global using Timer = System.Threading.Timer;
