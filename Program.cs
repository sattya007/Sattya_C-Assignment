//All the namespaces
using System.Text;
using System.Text.Json;
using ScottPlot;//Installed ScottPlot for PieChart

//Part A
//full API URL from the respective email
var client = new HttpClient();
string text = await client.GetStringAsync("https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==");
//Turns text into a list of TimeEntry objects
var entries = JsonSerializer.Deserialize<List<TimeEntry>>(text);
//Cross checking
Console.WriteLine(entries.Count);
Console.WriteLine(entries[0].EmployeeName);

// Data cleaning: the API data has some bad entries, so we skip them.
// 1. Will skip entries where EmployeeName is null (we can't tell who worked).
// 2. Will skip entries where DeletedOn has a date (the entry was deleted, so it shouldn't count).
// 3. Will skip entries where EndTimeUtc is before StarTimeUtc (gives negative hours, so the data is wrong).
var goodEntries = entries.Where(x => x.EmployeeName != null && x.DeletedOn == null && x.EndTimeUtc > x.StarTimeUtc);
//Cross checking
Console.WriteLine(goodEntries.Count());
//Now our task is to group , add then sort

//Group "Rita Alley":
//   entry 1: 07:22 → 08:50(1.47 hours)
//   entry 2: 08:22 → 09:28(1.10 hours)
//   entry 3: 07:48 → 10:16(2.47 hours)... more entries

//Select to calculate
//{ Name = "Rita Alley", TotalHours = 1.47 + 1.10 + 2.47 + ... }

var employees = goodEntries
    .GroupBy(x => x.EmployeeName)
    .Select(g => new { Name = g.Key, TotalHours = g.Sum(x => (x.EndTimeUtc - x.StarTimeUtc).TotalHours) })
    .OrderByDescending(e => e.TotalHours)
    .ToList();

//Cross checking
foreach (var e in employees)
{
    Console.WriteLine($"{e.Name}  {e.TotalHours:F2}");
}

// Trying to write the HTML page as text over here
var sb = new StringBuilder();
sb.Append("<html><head><title>Employees</title>");
sb.Append("<style>");
sb.Append("table { border-collapse: collapse; } th, td { border: 1px solid black; padding: 8px; }");
sb.Append(".low { background-color: #f8d7da; }");   // colour for under 100 hours
sb.Append("</style></head><body>");
sb.Append("<h2>Employees by Total Time Worked</h2>");
sb.Append("<table>");
sb.Append("<tr><th>Name</th><th>Total Hours</th></tr>");

// One row per employee; colour the row if under 100 hours
foreach (var e in employees)
{
    if (e.TotalHours < 100)
    {
        sb.Append("<tr class='low'>");
    }
    else
    {
        sb.Append("<tr>");
    }
    sb.Append($"<td>{e.Name}</td>");
    sb.Append($"<td>{e.TotalHours:F2}</td>");
    sb.Append("</tr>");
}

// Close & Save the HTML file
sb.Append("</table></body></html>");
File.WriteAllText("employees.html", sb.ToString());
Console.WriteLine("HTML file created: employees.html");

//Part B
// Pie chart: each employee's share of the total hours
var plt = new Plot();

// One slice per employee
var slices = new List<PieSlice>();
double total = employees.Sum(e => e.TotalHours);
foreach (var e in employees)
{
    double percent = e.TotalHours / total * 100;
    slices.Add(new PieSlice() { Value = e.TotalHours, Label = $"{e.Name} ({percent:F1}%)" });
}

var pie = plt.Add.Pie(slices);

plt.Title("Share of Total Time Worked");
plt.ShowLegend();      // list of names with colours
plt.HideGrid();        // pie charts don't need a grid
plt.Layout.Frameless();

plt.SavePng("piechart.png", 800, 600);
Console.WriteLine("Pie chart created: piechart.png");

//Class for time of entry
public class TimeEntry
{
    public string Id {get;set;}
    public string EmployeeName { get; set; }
    public string EntryNotes { get; set; }
    public DateTime StarTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public DateTime? DeletedOn { get; set; }
}
