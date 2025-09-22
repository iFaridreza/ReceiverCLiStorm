using System.Globalization;

Console.WriteLine("Hello, World!");

CultureInfo custom = new("en-US");
custom.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
CultureInfo.DefaultThreadCurrentCulture = custom;
CultureInfo.DefaultThreadCurrentUICulture = custom;

var x = DateOnly.FromDateTime(DateTime.Now);

Console.WriteLine(x);