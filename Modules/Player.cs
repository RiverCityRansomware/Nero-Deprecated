
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Discord;
using Discord.Commands;

namespace Nero {
    public class Player {
        public ulong discordID;
        public string fileName;
        public string playerName;
        public string dc;
        public string world;
        public List<Fight> fights;
        public List<job> jobs;
        public List<string> cleared;
        public double bestPercent;
        public double bestfightPercent = 0.0;
        public double bestSavagePercent;
        public double bestSavageDps;
        public double bestDps;
        public int fightsCleared;
        public string xivdbURL;
        public string xivdbURL_API;

        public double[] fight_dps;
        public ulong staticId;

        public Player(ulong _discordID, string _playerName, string _dc, string _world) {
            discordID = _discordID;
            playerName = _playerName;
            fileName = $"characters/{discordID}.json";
            fights = new List<Fight>();
            jobs = new List<job>();
            cleared = new List<string>();
            dc = _dc;
            world = _world;
            bestPercent = 0.0;
            bestDps = 0.0;
            bestfightPercent = 0.0;
            bestSavageDps = 0.0;
            bestSavagePercent = 0.0;
            fightsCleared = 0;
            xivdbURL = "";
            xivdbURL_API = "";
            fight_dps = new double[12];
            staticId = 0;
            this.EnsureExists();
        }

        public void AddFight(bool _cleared, int _kills, int _jobAmount, List<job> _jobs, string _fightName, double _bestDps, double _bestPercent) {
            fights.Add(new Fight(_cleared, _kills, _jobAmount, _jobs, _fightName, _bestDps, _bestPercent));
        }

        public void CalculateBest() {
            this.bestPercent = 0.0;
            Console.WriteLine("");
            foreach (var fight in fights) {
                if (fight.fightName != "Shinyru" && fight.fightName != "Susano" && fight.fightName != "Lakshmi" && fight.fightName != "Bahamut Prime") {
                    this.bestPercent += fight.bestPercent;
                    Console.Write($"fight: {fight.fightName} fight %: {fight.bestPercent}% | ");
                    if (this.bestDps <= fight.bestDps) {
                        this.bestDps = fight.bestDps;
                    }
                }
                

                switch (fight.fightName)
                    {
                        case "Alte Roite":
                        case "Catastrophe":
                        case "Halicarnassus":
                            if (fight.bestDps >= this.bestSavageDps)
                                this.bestSavageDps = fight.bestDps;
                            
                            if (fight.bestPercent >= this.bestSavagePercent)
                                this.bestSavagePercent = fight.bestPercent;
                            break;
                        case "Exdeath":
                            break;
                        case "Neo Exdeath":
                            if (fight.bestDps >= this.bestSavageDps)
                                this.bestSavageDps = fight.bestDps;

                            if (fight.bestPercent >= this.bestSavagePercent)
                                this.bestSavagePercent = fight.bestPercent;
                            break;
                        default:
                            break;
                    }
            }

            foreach(var job in jobs) { 
                if (fightsCleared > 0)
                    job.savageP /= fightsCleared;
            }

            
            if (fightsCleared > 0) {
                this.bestPercent /= fightsCleared;
                var tempPercent = this.bestPercent;
                this.bestPercent = Math.Min(100, tempPercent); 
            } else {
                this.bestPercent = 0.0;
            }
            Console.WriteLine("");
        }

        public string GetTopThreeDPS(Fight fight, ICommandContext context) {
            //Console.WriteLine(fight.fightName);
            if (context != null && context.Channel.GetType().Name != "SocketDMChannel") {
                var emoteResults =  from emo in context.Guild.Emotes
                                where emo.Name.ToLower().Contains("ast") || emo.Name.ToLower().Contains("blm") || emo.Name.ToLower().Contains("brd") ||
                                emo.Name.ToLower().Contains("drg") || emo.Name.ToLower().Contains("drk") || emo.Name.ToLower().Contains("mch") ||
                                emo.Name.ToLower() == "mnk" || emo.Name.ToLower().Contains("nin") || emo.Name.ToLower().Contains("pld") ||
                                emo.Name.ToLower().Contains("rdm") || emo.Name.ToLower().Contains("sam") || emo.Name.ToLower().Contains("sch") ||
                                emo.Name.ToLower().Contains("smn") || emo.Name.ToLower().Contains("war") || emo.Name.ToLower().Contains("whm") 
                                select emo;

                var reply = "";

                if (emoteResults.Count() > 12) {
                    var jobsSortedResults = (from job in fight.jobs
                                            orderby job.dps
                                            select job).Reverse().Take(3);
                    

                    if (jobsSortedResults.Count() > 0) {
                        foreach (var job in jobsSortedResults) {
                            foreach (var emote in emoteResults) {
                                if (job.short_name == emote.Name) {
                                    reply += $"{emote.ToString()} {job.dps}% ";
                                    //Console.WriteLine(reply);
                                }
                            }
                        }
                    }
                
                    jobsSortedResults = null;
                } else {
                    var jobsSortedResults = (from job in fight.jobs
                                            orderby job.historical_percent
                                            select job).Reverse().Take(3);

                    if (jobsSortedResults.Count() > 0) {
                        foreach (var job in jobsSortedResults) {
                            reply += $":{job.short_name}: {job.historical_percent}% ";
                            //Console.WriteLine(reply);
                            
                        
                        }
                    }
                }


                return reply;
            } else {
                var reply = "";

                var jobsSortedResults = (from job in fight.jobs
                                            orderby job.dps
                                            select job).Take(3);

                    if (jobsSortedResults.Count() > 0) {
                        foreach (var job in jobsSortedResults) {
                            reply += $":{job.short_name}: {Math.Floor(job.dps)}% ";
                            //Console.WriteLine(reply);
                            
                        
                        }
                    }
                    return reply;
                }
            }
        


public List<string> GetClearedFights(){
            var clearedFightsList = new List<string>();
            fightsCleared = 0;
            var jobnames = new List<string>();

            foreach (Fight fight in fights) {

                foreach (var job in fight.jobs) {
                    foreach(var j in jobs) {
                        if (j.name == job.name) {
                            if (fight.fightName == "Alte Roite" || fight.fightName == "Catastrophe" || fight.fightName == "Halicarnassus" || fight.fightName == "Neo Exdeath") {
                                j.savageP += job.historical_percent;
                            }
                        }
                    }
                    
                    if (jobnames.Contains(job.name) == false) {
                        jobnames.Add(job.name);
                        jobs.Add(job);

                    foreach(var j in jobs) {
                        if (j.name == job.name) {
                            if (fight.fightName == "Alte Roite" || fight.fightName == "Catastrophe" || fight.fightName == "Halicarnassus" || fight.fightName == "Neo Exdeath") {
                                j.savageP += job.historical_percent;
                            }
                        }
                    }
                    } else {
                        foreach (var jb in this.jobs) {
                            if (jb.name == job.name && jb.historical_percent <= job.historical_percent) {
                                jb.historical_percent = job.historical_percent;
                            }
                            if (jb.name == job.name && jb.historical_dps <= job.historical_dps) {
                                jb.historical_dps = job.historical_dps;
                            }

                            
                        }
                    }
                    
                }
            
            if (fight.cleared == true && clearedFightsList.Contains(fight.fightName) == false) {
                    switch (fight.fightName)
                    {
                        case "Alte Roite":
                            clearedFightsList.Add($"O1S");
                            cleared.Add("O1S");
                            fightsCleared++;
                            break;
                        case "Catastrophe":
                            clearedFightsList.Add($"O2S");
                            cleared.Add("O2S");
                            fightsCleared++;
                            break;
                        case "Halicarnassus":
                            clearedFightsList.Add($"O3S");
                            cleared.Add("O3S");
                            fightsCleared++;
                            break;
                        case "Exdeath":
                            break;
                        case "Neo Exdeath":
                            clearedFightsList.Add($"O4S");
                            cleared.Add("O4S");
                            fightsCleared++;
                            break;
                        case "Shinryu":
                            var shinryuReply = $"\n**__Trial__**\nShinryu";
                            clearedFightsList.Add(shinryuReply);
                            cleared.Add("Shinryu");
                            break;
                        case "Bahamut Prime":
                            clearedFightsList.Add("Bahamut");
                            cleared.Add("Bahamut");
                            break;
                        case "Susano":
                            break;
                        case "Lakshmi":
                            break;
                        default:
                            cleared.Add(fight.fightName);
                            fightsCleared++;
                            break;
                    }

                }

            }
            this.CalculateBest();
            this.EnsureExists();
            return clearedFightsList;
        }


        public List<string> GetClearedFights(ICommandContext context){
            var clearedFightsList = new List<string>();
            fightsCleared = 0;
            var jobnames = new List<string>();

            foreach (Fight fight in fights) {

                foreach (var job in fight.jobs) {
                    foreach(var j in jobs) {
                        if (j.name == job.name) {
                            if (fight.fightName == "Alte Roite" || fight.fightName == "Catastrophe" || fight.fightName == "Halicarnassus" || fight.fightName == "Neo Exdeath") {
                                j.savageP += job.historical_percent;
                            }
                        }
                    }
                    
                    if (jobnames.Contains(job.name) == false) {
                        jobnames.Add(job.name);
                        jobs.Add(job);

                    foreach(var j in jobs) {
                        if (j.name == job.name) {
                            if (fight.fightName == "Alte Roite" || fight.fightName == "Catastrophe" || fight.fightName == "Halicarnassus" || fight.fightName == "Neo Exdeath") {
                                j.savageP += job.historical_percent;
                            }
                        }
                    }
                    } else {
                        foreach (var jb in this.jobs) {
                            if (jb.name == job.name && jb.historical_percent <= job.historical_percent) { 
                                jb.historical_percent = job.historical_percent;
                            }
                            if (jb.name == job.name && jb.historical_dps <= job.historical_dps) {
                                jb.historical_dps = job.historical_dps;
                            }

                        }
                    }
                    
                }
            
            if (fight.cleared == true && clearedFightsList.Contains(fight.fightName) == false) {
                    switch (fight.fightName) {
                        case "Alte Roite":
                            clearedFightsList.Add($"O1S {this.GetTopThreeDPS(fight, context)}");
                            cleared.Add("O1S");
                            fightsCleared++;
                            break;
                        case "Catastrophe":
                            clearedFightsList.Add($"O2S {this.GetTopThreeDPS(fight, context)}");
                            cleared.Add("O2S");
                            fightsCleared++;
                            break;
                        case "Halicarnassus":
                            clearedFightsList.Add($"O3S {this.GetTopThreeDPS(fight, context)}");
                            cleared.Add("O3S");
                            fightsCleared++;
                            break;
                        case "Exdeath":
                            break;
                        case "Neo Exdeath":
                            clearedFightsList.Add($"O4S {this.GetTopThreeDPS(fight, context)}");
                            cleared.Add("O4S");
                            fightsCleared++;
                            break;
                        case "Shinryu":
                            var shinryuReply = $"\n**__Trial__**\n  - Shinryu {this.GetTopThreeDPS(fight, context)}";
                            clearedFightsList.Add(shinryuReply);
                            cleared.Add("Shinryu");
                            //dont increment fightsCleared
                            break;
                        case "Bahamut Prime":
                            clearedFightsList.Add("Bahamut");
                            cleared.Add("Bahamut");
                            break;
                        case "Susano":
                            break;
                        case "Lakshmi":
                            break;
                        default:
                            cleared.Add(fight.fightName);
                            fightsCleared++;
                            break;
                    }

                    
                }

            }
            this.CalculateBest();
            this.EnsureExists();
            return clearedFightsList;
        }

        public List<string> GetSavageJobs() {
            var savageJobs = new List<string>();

            foreach (var job in this.jobs) {
                if(savageJobs.Contains(job.name) == false) {
                    if(job.savageP >= 95.0) {
                        savageJobs.Add(job.name);
                        Console.WriteLine($"savage job {job.name} added, job hist%: {job.historical_percent}, savage %: {job.savageP}, player: {this.bestPercent}");
                    }
                }
            }

            return savageJobs;
        }

        public void EnsureExists() {
            string file = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(file)) {
                string path = Path.GetDirectoryName(file);
                if(!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                this.SaveJson();
            } else {
                this.SaveJson();
            }
        }

        public void SaveJson() {
            string file = Path.Combine(AppContext.BaseDirectory, fileName);
            File.WriteAllText(file, ToJson());
        }

        public static Player Load(ulong id) {
            string file = Path.Combine(AppContext.BaseDirectory, $"characters/{id}.json");
            return JsonConvert.DeserializeObject<Player>(File.ReadAllText(file));
        }

        public static bool DoesProfileExist(ulong id) {
            string file = Path.Combine(AppContext.BaseDirectory, $"characters/{id}.json");

            if (!File.Exists(file)) {
                return false;
            } else {
                return true;
            }
        }

        public string ToJson() 
            => JsonConvert.SerializeObject(this, Formatting.Indented);

    }
}