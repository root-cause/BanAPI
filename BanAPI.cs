using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using Newtonsoft.Json;

namespace BanAPI
{
    public enum BanType
    {
        Name,
        SocialClubName,
        IP,
        HWID
    }

    public class Ban
    {
        public string ID;
        public int Type;
        public string Value;
        public string Reason;
        public DateTime? EndDate;

        public Ban(string id, int type, string value, string reason, DateTime? enddate)
        {
            ID = id;
            Type = type;
            Value = value;
            Reason = reason;
            EndDate = enddate;
        }
    }

    public class BanAPI : Script
    {
        List<Ban> Bans = null;

        public BanAPI()
        {
            API.onResourceStart += BanAPI_Init;
            API.onPlayerConnected += BanAPI_PlayerConnected;
            API.onResourceStop += BanAPI_Exit;
        }

        #region Methods
        public string GetBanFile()
        {
            return API.getResourceFolder() + Path.DirectorySeparatorChar + "bans.json";
        }

        public Ban GetPlayerBan(Client player)
        {
            return Bans.FirstOrDefault(b => b.Value == player.name || b.Value == player.socialClubName || b.Value == player.address || b.Value == player.uniqueHardwareId);
        }

        public void SaveBans()
        {
            File.WriteAllText(GetBanFile(), JsonConvert.SerializeObject(Bans, Formatting.Indented));
        }

        public bool IsIDInUse(string ID)
        {
            return (Bans.FirstOrDefault(b => b.ID == ID) != null);
        }

        public void BanCheck(Client player)
        {
            Ban ban = GetPlayerBan(player);
            if (ban == null) return;

            if (ban.EndDate > DateTime.Now)
            {
                player.kick(string.Format("You're banned.~n~~n~~b~Ban ID: ~w~{0}~n~~b~Reason: ~w~{1}~n~~b~Ends On: ~w~{2}", ban.ID, ban.Reason, ((ban.EndDate == null) ? "Permanent" : ban.EndDate.Value.ToString("dd/MM/yyyy HH:mm:ss"))));
            }
            else
            {
                player.sendChatMessage("Your ban has expired.");

                Bans.Remove(ban);
                SaveBans();
            }
        }
        #endregion

        #region Exported Methods
        public bool IsPlayerBanned(Client player)
        {
            Ban ban = GetPlayerBan(player);
            if (ban == null) return false;

            return (ban.EndDate == null) ? true : (ban.EndDate > DateTime.Now);
        }

        public bool IsValueBanned(string value)
        {
            Ban ban = Bans.FirstOrDefault(b => b.Value == value);
            if (ban == null) return false;

            return (ban.EndDate == null) ? true : (ban.EndDate > DateTime.Now);
        }

        public bool IsValueBannedWithType(string value, int type)
        {
            Ban ban = Bans.FirstOrDefault(b => b.Type == type && b.Value == value);
            if (ban == null) return false;

            return (ban.EndDate == null) ? true : (ban.EndDate > DateTime.Now);
        }

        public string[] GetBanInfo(string ban_ID)
        {
            Ban ban = Bans.FirstOrDefault(b => b.ID == ban_ID);
            return (ban == null) ? null : new string[] { ((BanType)ban.Type).ToString(), ban.Value, ban.Reason, ((ban.EndDate == null) ? "Permanent" : ban.EndDate.Value.ToString("dd/MM/yyyy HH:mm:ss")) };
        }

        public string BanPlayer(Client player, int ban_type, string reason, int days)
        {
            if (IsPlayerBanned(player)) return null;
            if (!Enum.IsDefined(typeof(BanType), ban_type)) return null;

            string value = player.name;
            bool check_others = false;

            switch ((BanType)ban_type)
            {
                case BanType.Name:
                    value = player.name;
                    break;

                case BanType.SocialClubName:
                    value = player.socialClubName;
                    if (!API.getServerSocialClubDuplicateSetting()) check_others = true;
                    break;

                case BanType.IP:
                    value = player.address;
                    check_others = true;
                    break;

                case BanType.HWID:
                    value = player.uniqueHardwareId;
                    if (!API.getServerHwidDuplicateSetting()) check_others = true;
                    break;
            }

            if (string.IsNullOrEmpty(reason)) reason = "No reason given.";

            string ban_ID = string.Empty;

            do
            {
                ban_ID = RandomIdGenerator.GetBase36(6);
            } while (IsIDInUse(ban_ID));

            if (days < 1)
            {
                Bans.Add(new Ban(ban_ID, ban_type, value, reason, null));
            }
            else
            {
                Bans.Add(new Ban(ban_ID, ban_type, value, reason, DateTime.Now.AddDays(days)));
            }

            if (check_others)
            {
                List<Client> players = new List<Client>();

                switch ((BanType)ban_type)
                {
                    case BanType.SocialClubName:
                        players = API.getAllPlayers().Where(p => p.socialClubName == value).ToList();
                        break;

                    case BanType.IP:
                        players = API.getAllPlayers().Where(p => p.address == value).ToList();
                        break;

                    case BanType.HWID:
                        players = API.getAllPlayers().Where(p => p.uniqueHardwareId == value).ToList();
                        break;
                }

                foreach (Client p in players) p.kick(string.Format("You're banned.~n~~n~~b~Ban ID: ~w~{0}~n~~b~Reason: ~w~{1}~n~~b~Ends On: ~w~{2}", ban_ID, reason, ((days < 1) ? "Permanent" : DateTime.Now.AddDays(days).ToString("dd/MM/yyyy HH:mm:ss"))));
            }
            else
            {
                player.kick(string.Format("You're banned.~n~~n~~b~Ban ID: ~w~{0}~n~~b~Reason: ~w~{1}~n~~b~Ends On: ~w~{2}", ban_ID, reason, ((days < 1) ? "Permanent" : DateTime.Now.AddDays(days).ToString("dd/MM/yyyy HH:mm:ss"))));
            }
            
            SaveBans();
            return ban_ID;
        }

        public string BanValue(string value, int ban_type, string reason, int days)
        {
            if (IsValueBannedWithType(value, ban_type)) return null;
            if (!Enum.IsDefined(typeof(BanType), ban_type)) return null;

            if (string.IsNullOrEmpty(reason)) reason = "No reason given.";

            string ban_ID = string.Empty;

            do
            {
                ban_ID = RandomIdGenerator.GetBase36(6);
            } while (IsIDInUse(ban_ID));

            if (days < 1)
            {
                Bans.Add(new Ban(ban_ID, ban_type, value, reason, null));
            }
            else
            {
                Bans.Add(new Ban(ban_ID, ban_type, value, reason, DateTime.Now.AddDays(days)));
            }

            SaveBans();
            return ban_ID;
        }

        public bool Unban(string ban_ID)
        {
            Ban ban = Bans.FirstOrDefault(b => b.ID == ban_ID);
            if (ban == null) return false;

            Bans.Remove(ban);
            SaveBans();
            return true;
        }
        #endregion

        #region Events
        public void BanAPI_Init()
        {
            // verify bans.json
            string ban_file = GetBanFile();
            if (!File.Exists(ban_file)) File.Create(ban_file).Close();

            // load all bans
            Bans = JsonConvert.DeserializeObject<List<Ban>>(File.ReadAllText(ban_file));
            if (Bans == null) Bans = new List<Ban>();

            // remove the expired bans
            if (Bans.RemoveAll(b => b.EndDate != null && b.EndDate < DateTime.Now) > 0) SaveBans();

            // check already connected players
            foreach (Client player in API.getAllPlayers()) BanCheck(player);

            // done
            API.consoleOutput("BanAPI: {0} bans loaded.", Bans.Count);
        }

        public void BanAPI_PlayerConnected(Client player)
        {
            BanCheck(player);
        }

        public void BanAPI_Exit()
        {
            Bans.Clear();
        }
        #endregion
    }

    // https://stackoverflow.com/a/9543797
    #region RandomIdGenerator
    public static class RandomIdGenerator
    {
        private static char[] _base62chars =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            .ToCharArray();

        private static Random _random = new Random();

        public static string GetBase62(int length)
        {
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
                sb.Append(_base62chars[_random.Next(62)]);

            return sb.ToString();
        }

        public static string GetBase36(int length)
        {
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
                sb.Append(_base62chars[_random.Next(36)]);

            return sb.ToString();
        }
    }
    #endregion
}