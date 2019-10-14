using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;


namespace LodestoneReader
{
    public class LodestoneReader
    {
        static readonly HttpClient client = new HttpClient();
        private string fullHTML;
        public Dictionary<string, int> CharacterStats;
        private List<string> stats = new List<string>()
        {
                {"Strength"},
                {"Dexterity"},
                {"Vitality"},
                {"Intelligence"},
                {"Mind"},
                {"Critical Hit Rate"},
                {"Determination"},
                {"Direct Hit Rate"},
                {"Defense"},
                {"Magic Defense"},
                {"Attack Power"},
                {"Skill Speed"},
                {"Attack Magic Potency"},
                {"Healing Magic Potency"},
                {"Spell Speed"},
                {"Tenacity"},
                {"Piety"},
                {"Average Item Level"},
        };
        private List<string> combatJobs = new List<string>()
        {
            {"Astrologian"},
            {"Bard"},
            {"Black Mage"},
            {"Blue Mage"},
            {"Dancer"},
            {"Dark Knight"},
            {"Dragoon"},
            {"Gunbreaker"},
            {"Machinist"},
            {"Monk"},
            {"Ninja"},
            {"Paladin"},
            {"Red Mage"},
            {"Samurai"},
            {"Scholar"},
            {"Summoner"},
            {"Warrior"},
            {"White Mage"},
        };

        private List<string> noncombatJobs = new List<string>()
        {
            {"Alchemist"},
            {"Armorer"},
            {"Blacksmith"},
            {"Botanist"},
            {"Carpenter"},
            {"Culinarian"},
            {"Goldsmith"},
            {"Fisher"},
            {"Leatherworker"},
            {"Miner"},
            {"Weaver"},

        };

        public string ConnectionStatus;
        public string ConnectionError;

        public LodestoneReader()
        {
            ConnectionStatus = "none";
            CharacterStats = new Dictionary<string, int>();
            ResetCharacterData();
        }
        public async Task Read(int charID)
        {
            
            string url = "https://na.finalfantasyxiv.com/lodestone/character/" + charID.ToString();


            Task<string> getHTML =  client.GetStringAsync(url);
            ConnectionStatus = "Attempting";
            try
            {
                fullHTML = await getHTML;
                GetCharacterStats();
                ConnectionStatus = "Success";
            }
            catch(Exception e)
            {
                ConnectionStatus = "Failed";
                ResetCharacterData();
                if (getHTML.IsFaulted)
                {
                    ConnectionError = getHTML.Exception.InnerException.Message;
                    
                }
                else
                {
                    ConnectionError = e.InnerException.Message;
                }
            }
            
        }

        public async Task Read(string firstName, string lastName, string serverName)
        {
            int charID = await GetCharacterID(firstName, lastName, serverName);
            if (charID == -1)
            {
                ConnectionStatus = "Failed";
                ConnectionError = "Could not retrieve characterID";
                ResetCharacterData();
                return;
            }

            await Read(charID);

        }

        public async Task<int> GetCharacterID(string firstName, string lastName, string serverName)
        {
            int tID = -1;
            string tHTML = "";
            int nameLocation,begin,end;

            string url = "https://na.finalfantasyxiv.com/lodestone/character/?q=" +  Uri.EscapeDataString(firstName) + "+" + Uri.EscapeDataString(lastName) + "&worldname=" + Uri.EscapeDataString(serverName);

            Task<string> getHTML = client.GetStringAsync(url);

            try
            {
                tHTML = await getHTML;           
            }
            catch (Exception e)
            {
                return - 1;
            }

            nameLocation = tHTML.IndexOf("alt=\"" + firstName + " " + lastName);
            if (nameLocation == -1) // not found
            {
                return -1;
            }
            begin = tHTML.LastIndexOf("/lodestone/character/", nameLocation) + 21;
            if (begin == -1) // not found
            {
                return -1;
            }
            end = tHTML.IndexOf("/", begin + 2);
            Int32.TryParse(tHTML.Substring(begin, end-begin),out tID);

            return tID;
        }

        private void GetCharacterStats()
        {
            
            string attrMiddle = @"</span></th><td"; // ends in  <td> or <td class="pb-0"> 
            string attrEnd = @"</td>";
            string subStr;
            int statBegin, statEnd;
            int statValue;

            foreach (string stat in stats)
            {
                statBegin = fullHTML.IndexOf(stat + attrMiddle);
                if (statBegin > 0)
                {
                    statBegin += attrMiddle.Length + stat.Length;
                    statEnd = fullHTML.IndexOf(attrEnd, statBegin);
                    subStr = fullHTML.Substring(statBegin, statEnd - statBegin);
                    subStr = subStr.Replace(">", "");
                    subStr = subStr.Replace(" class=\"pb-0\"", "");
                    statValue = 0;
                    Int32.TryParse(subStr, out statValue);
                    CharacterStats[stat] = statValue;
                }
                
            }

            statBegin = fullHTML.IndexOf("character__detail__avg"); // Item Level doesn't always work
            if (statBegin > 0)
            {
                subStr = fullHTML.Substring(statBegin + 24, 5);
                statValue = 0;
                Int32.TryParse(subStr, out statValue);
                CharacterStats["Average Item Level"] = statValue;
            }
            
                return;
        }

        private void ResetCharacterData()
        {
            foreach (string stat in stats)
            {
                CharacterStats[stat] = 0;
            }
        }
    }
}
