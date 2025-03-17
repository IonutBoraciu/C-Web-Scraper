using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data.SQLite;
using System.Net;

class db_request
{
    static string[] skills = {"C", "C++", "C#", "Java", "Python", "JIRA", "XNNG", "HTML", "CSS", "English", "France" , "SQL", 
                                "Github", "Git", "linux", "networking", "TCP", "UDP", "CCNA","REST","GraphQL","Angular","HTTP", "Big Data",
                                "Machine Learning", "ML", "Infrastructure", "Threat Hunt", "Reverse engineering", "Malware", "limbaj de programare", "Testarea manuala",
                                "loguri", "OOP", "SDLC", ".NET", "testing tools", "German", "Proactive", "relentless mindset", "fast learner", "Security",
                                "SQLPlus", "noSQL", "database", "JavaScript", "ES6+", "React", "Vue", "dark web monitoring", "OSINT", "TTP", "Node.js", "Rust",
                                "PHP", "Kotlin", "Swift", "TypeScript", "SOA", "AI", "LLM"};
    static string[] orase = {"București", "Iași", "Bucuresti", "Iasi"};
    static string[] statusPaid = {"Paid", "Unpaid"};
    static async Task init_db(dataBase db) {
        int nOfPages = -1;
        for(int i = 1; i <= nOfPages || nOfPages == -1; i++) {
            Console.WriteLine(i);
            string url = $"https://stagiipebune.ro/students/jobs/?search=&category=&location=&page={i}";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            try
            {
                string html = await client.GetStringAsync(url);
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
    
                var jobRows = htmlDoc.DocumentNode.SelectNodes("//td[contains(@class, 'job-row')]");
                if (jobRows != null) {
                    foreach (var job in jobRows)
                    {
                        var jobName = job.SelectSingleNode(".//a[contains(@class, 'color-emphasis')]");
                        var state = job.SelectSingleNode(".//p[@class='job-row-sub']/span[@class='muted']");
                        var companyName = job.SelectSingleNode(".//a[contains(@class,'color-link')]");
                        var cityHTML = job.SelectSingleNode(".//p[@class='job-row-sub']/span[@class='muted'][last()]");
                        var paginator = htmlDoc.DocumentNode.SelectSingleNode(".//span[@class='paginator-pages'][last()]");
                        string pageVector = paginator.InnerText.Trim();
                        if(nOfPages == -1)
                            nOfPages = pageVector[pageVector.Length - 1] - '0';
                        if(nOfPages <= 0) {
                            nOfPages = 1;
                        }
                        string state_paid = state.InnerText.Trim();
                        string title = companyName.InnerText.Trim();
                        string city = cityHTML.InnerText.Trim();
                        string jobPosition = "";

                        if (jobName != null) {

                            jobPosition = WebUtility.HtmlDecode(jobName.InnerText.Trim());
                            string? href = jobName.GetAttributeValue("href","not found");
                            string job_site = "https://stagiipebune.ro" + href;
                            string jobPage;
                            jobPage = await client.GetStringAsync(job_site);
                            HtmlDocument htmlJob = new HtmlDocument();
                            htmlJob.LoadHtml(jobPage);
                            var jobDescriptionNode = htmlJob.DocumentNode.SelectSingleNode("//div[contains(@class, 'job-detail-body')]");
                            string jobDescription = jobDescriptionNode != null ? jobDescriptionNode.InnerText.Trim() : "";

                            var foundSkills = skills.AsParallel()
                                .Where(skill =>
                                {
                                    string escapedSkill = Regex.Escape(skill.Trim()); 
                                    string pattern = $@"(?<!\w){escapedSkill}(?!\w)";
                                    return Regex.IsMatch(jobDescription, pattern, RegexOptions.IgnoreCase);
                                })
                                .ToList(); 
                            string skillsList = string.Join(", ", foundSkills);
                            db.insert(title, jobPosition, skillsList, state_paid, city);
                        }
                    }
                } else {
                    Console.WriteLine("No data was found for the job");
                }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        
        client.Dispose();
        }
    }

    static async Task Main()
    {
        // init the string by default with "" if something breaks
        dataBase db = dataBase.getInstance();
        int nOfEntries = db.countEntries();
        string command = "";

        db.helloMessage();

        while (!command.Equals("EXIT", StringComparison.OrdinalIgnoreCase)) {
            command = Console.ReadLine()  ?? ""; 
            if (command.Equals("INIT_DB", StringComparison.OrdinalIgnoreCase)) {
                db.reset();
                Console.WriteLine("");
                Console.WriteLine("INITIALIZING, PLEASE WAIT!");
                await init_db(db);
                Console.WriteLine("DONE!");
                Console.WriteLine("");
                nOfEntries = db.countEntries();
            } else if (command.Equals("SELECT ALL", StringComparison.OrdinalIgnoreCase)) {
                db.executeSelect("SELECT * FROM JOBS");
                Console.WriteLine("");
            } else if (command.Equals("SELECT", StringComparison.OrdinalIgnoreCase)) {
                int trackNumber = db.processInputParams(skills,orase,statusPaid);
                float percentage = (float)(trackNumber * 100)/nOfEntries;
                Console.WriteLine("Found " + trackNumber + " matches" + " from " + nOfEntries + " available (" + percentage + "%)");
                Console.WriteLine("");
                
            } else if(!command.Equals("EXIT",StringComparison.OrdinalIgnoreCase)){
                Console.WriteLine("INVALID COMMAND!");
            }
        }
        Console.WriteLine("GOODBYE!");
        
    }
}
