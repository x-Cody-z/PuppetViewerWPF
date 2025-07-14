using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PuppetViewerWPF;
//using static System.Net.Mime.MediaTypeNames;

namespace PuppetViewerWPF
{
    /// <summary>
    /// Interaction logic for Calc.xaml
    /// </summary>
    public partial class Calc : Window
    {
        private MainWindow _mainWindow;
        private List<string[]> _playerData;
        private List<string[]> _enemyData;

        private List<Button> _pButtons;
        private List<Button> _eButtons;

        private List<Run> _pSkillNameRuns;
        private List<Run> _eSkillNameRuns;

        private List<Run> _pSkillInfoRuns;
        private List<Run> _eSkillInfoRuns;

        private int _playerIndex = 0;
        private int _enemyIndex = 0;

        public Calc(MainWindow mw, List<string[]> pData, List<string[]> eData)
        {
            InitializeComponent();

            _mainWindow = mw;
            _playerData = pData;
            _enemyData = eData;

            // Initialize buttons for player and enemy puppets
            _pButtons = new List<Button>
             {
                 P1, P2, P3, P4, P5, P6
             };
            _eButtons = new List<Button>
             {
                 E1, E2, E3, E4, E5, E6
             };

            // Assign click events and tags for Player side
            for (int i = 0; i < _pButtons.Count; i++)
            {
                _pButtons[i].Tag = (i, "Player");
            }

            // Assign click events and tags for Enemy side
            for (int i = 0; i < _eButtons.Count; i++)
            {
                _eButtons[i].Tag = (i, "Enemy");
            }

            // Initialize skill name runs for Player and Enemy
            _pSkillNameRuns = new List<Run>
            {
                P_Skill_Name1, P_Skill_Name2, P_Skill_Name3, P_Skill_Name4
            };
            _eSkillNameRuns = new List<Run>
            {
                E_Skill_Name1, E_Skill_Name2, E_Skill_Name3, E_Skill_Name4
            };

            // Initialize skill info runs for Player and Enemy
            _pSkillInfoRuns = new List<Run>
            {
                P_Skill_Info1, P_Skill_Info2, P_Skill_Info3, P_Skill_Info4
            };
            _eSkillInfoRuns = new List<Run>
            {
                E_Skill_Info1, E_Skill_Info2, E_Skill_Info3, E_Skill_Info4
            };

            updateUI();
            updateUI(0, "Enemy");

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

        }

        private void updateButtonUI()
        {
            if (_playerData != null && _enemyData != null)
            {
                for (int i = 0; i < _playerData.Count; i++)
                {
                    // Assuming you have TextBlocks or other UI elements to display player data
                    // Example: PlayerNameTextBlock.Text = _playerData[i][0]; // Replace with actual index and element

                    //updateing the buttons with an image of the puppet
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    string imgPath = System.IO.Path.Combine(exeDir, "img", _playerData[i][0].PadLeft(3, '0') + "_00.png");
                    Image img = new Image
                    {
                        Stretch = Stretch.Uniform
                    };
                    try
                    {
                        img.Source = new BitmapImage(new Uri(imgPath, UriKind.Absolute));
                    }
                    catch
                    {
                        img.Source = new BitmapImage(new Uri(System.IO.Path.Combine(exeDir, "img", "000_00.png"), UriKind.Absolute));
                    }

                    _pButtons[i].Content = img;
                }

                // Update enemy buttons similarly
                for (int i = 0; i < _enemyData.Count; i++)
                {
                    // Assuming you have TextBlocks or other UI elements to display enemy data
                    // Example: EnemyNameTextBlock.Text = _enemyData[i][0]; // Replace with actual index and element
                    // Updating the buttons with an image of the puppet
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    string imgPath = System.IO.Path.Combine(exeDir, "img", _enemyData[i][0].PadLeft(3, '0') + "_00.png");
                    Image img = new Image
                    {
                        Stretch = Stretch.Uniform
                    };
                    try
                    {
                        img.Source = new BitmapImage(new Uri(imgPath, UriKind.Absolute));
                    }
                    catch
                    {
                        img.Source = new BitmapImage(new Uri(System.IO.Path.Combine(exeDir, "img", "000_00.png"), UriKind.Absolute));
                    }
                    _eButtons[i].Content = img;
                }
            }
        }

        private void updateStatsUI(int index = 0, string side = "Player")
        {
            int[] stats = getStats(index, side);

            // Assuming you have TextBlocks or other UI elements to display stats
            // Example: PlayerHPTextBlock.Text = stats[0].ToString(); // Replace with actual index and element
            if (side == "Player")
            {
                P_HP.Text = stats[0].ToString();
                P_FOA.Text = stats[1].ToString();
                P_FOD.Text = stats[2].ToString();
                P_SPA.Text = stats[3].ToString();
                P_SPD.Text = stats[4].ToString();
                P_SPE.Text = stats[5].ToString();
            }
            else
            {
                E_HP.Text = stats[0].ToString();
                E_FOA.Text = stats[1].ToString();
                E_FOD.Text = stats[2].ToString();
                E_SPA.Text = stats[3].ToString();
                E_SPD.Text = stats[4].ToString();
                E_SPE.Text = stats[5].ToString();
            }
        }

        private void updateSkillsUI()
        {
            //element1, element2
            //string[] puppetTypes = _mainWindow.GetPuppetData();   <-- types from pupppet_id and style_index


            //id, name, element, type, sp, accuracy, power, prio, effectchance, effectid, effecttarget, ynk classificaiton
            //string[] skillData = _mainWindow.GetSkillData();   <--- skill by skill id

            //get the 4 skills for the selected puppet, get the data for each, get the types of atk and def puppet, pass into damage calc for each skill and each side

            for (int i = 0; i < 4; i++)
            {
                //updateing player side skills
                if (_playerData[_playerIndex][i + 19] != "0")
                {
                    string[] skilldata = (_mainWindow.GetSkillData(_playerData[_playerIndex][i+19]));
                    string[] dmgs = calculateDamage(skilldata, "Player");

                    //update skill name for player
                    _pSkillNameRuns[i].Text = skilldata[1] + "     "; // Skill name
                    //update skill info for player, formated e.g. "86.5% - 93.0%"
                    _pSkillInfoRuns[i].Text = $"{dmgs[2]} - {dmgs[3]}";
                }
                else
                {
                    _pSkillNameRuns[i].Text = "None";
                    _pSkillInfoRuns[i].Text = "";
                }

                //updateing enemy side skills
                if (_enemyData[_enemyIndex][i + 19] != "0")
                {
                    string[] skilldata = (_mainWindow.GetSkillData(_enemyData[_enemyIndex][i+19]));
                    string[] dmgs = calculateDamage(skilldata, "Enemy");
                    //update skill name for enemy
                    _eSkillNameRuns[i].Text = skilldata[1] + "     "; // Skill name
                    //update skill info for enemy, formated e.g. "86.5% - 93.0%"
                    _eSkillInfoRuns[i].Text = $"{dmgs[2]} - {dmgs[3]}";
                }
                else
                {
                    _eSkillNameRuns[i].Text = "None";
                    _eSkillInfoRuns[i].Text = "";
                }
            }

        }

        private void updateUI(int index = 0, string side = "Player")
        {
            updateButtonUI();
            updateStatsUI(index,side);
            updateSkillsUI();
            
        }

        private int calcStat(int baseStat, int level, int iv, int ev, double markMod)
        {
            // Calculate the stat based on the formula: 
            // Stat = (((Base * 2 + IV + (EV / 4)) * Level) / 100) + Level + 5) * NatureModifier

            //mark = MARKS[pupp.find(".mark").val()] == statName ? 1.1 : 
            return (int) Math.Floor((Math.Floor((2 * (baseStat + iv) + ev) * level / 100.0) + 5) * markMod);
        }

        private int calcHP(int baseHP, int level, int iv, int ev)
        {
            // Calculate the HP based on the formula: 
            // HP = ((Base * 2 + IV + (EV / 4)) * Level) / 100 + Level + 10
            if (baseHP == 1)
            {
                return 1;
            }
            else
            {
                return (int) Math.Floor((2 * (baseHP + iv) + ev) * level / 100.0) + level + 10;
            }
        }

        private int[] intToBinarytoint(int value)
        {
            //convert an int to 8 digit binary value, then convert that to two int values
            string binary = Convert.ToString(value, 2).PadLeft(8, '0');
            int[] result = new int[2];
            result[0] = Convert.ToInt32(binary.Substring(0, 4), 2); // First 4 bits
            result[1] = Convert.ToInt32(binary.Substring(4, 4), 2); // Last 4 bits
            //MessageBox.Show($"Converted {value} to binary: {binary} -> [{result[0]}, {result[1]}]");

            return result;
        }

        private int[] getStats(int index, string side)
        {
            List<string[]> data = side == "Player" ? _playerData : _enemyData;

            //pupppet data should be:
            //puppet_id, style_index, held_item, ability_index, puppet_nickname, level, mark, , ev0, ev1, ev2, ev3, ev4, ev5, ,iv0, iv1, iv2, , skill0, skill1, skill2, skill3

            //get base stats for the selected puppet
            List<int> basestats = _mainWindow.GetPuppetBaseStats(data[index][0], data[index][1]);
            //get ivs for the selected puppet
            List<int> ivsList = new List<int>();
            for (int i = 15; i < 18; i++)
            {
                int[] ivPair = intToBinarytoint(Convert.ToInt32(data[index][i]));
                ivsList.Add(ivPair[0]);
                ivsList.Add(ivPair[1]);
            }
            //get evs for the selected puppet
            List<int> evsList = new List<int>();
            for (int i = 8; i < 14; i++)
            {
                evsList.Add(Convert.ToInt32(data[index][i]));
            }
            //get level for the selected puppet
            int level = Convert.ToInt32(data[index][5]);
            //get mark for the selected puppet
            int markid = Convert.ToInt32(data[index][6]);

            //calculate hp stat
            int hp = calcHP(basestats[0], level, ivsList[0], evsList[0]);

            //calculate other stats
            int atk = calcStat(basestats[1], level, ivsList[1], evsList[1], markid == 1 ? 1.1 : 1.0);
            int def = calcStat(basestats[2], level, ivsList[2], evsList[2], markid == 2 ? 1.1 : 1.0);
            int spa = calcStat(basestats[3], level, ivsList[3], evsList[3], markid == 3 ? 1.1 : 1.0);
            int spd = calcStat(basestats[4], level, ivsList[4], evsList[4], markid == 4 ? 1.1 : 1.0);
            int spe = calcStat(basestats[5], level, ivsList[5], evsList[5], markid == 5 ? 1.1 : 1.0);

            //return stats as a int array
            int[] stats = new int[] { hp, atk, def, spa, spd, spe };
            return stats;

        }

        private string[] calculateDamage(string[] skillData, string side)
        {
            int[] pStats = getStats(_playerIndex, "Player");
            int[] eStats = getStats(_enemyIndex, "Enemy");

            float atkModifiers = 1.0f; // Example value, replace with actual input
            float defModifiers = 1.0f; // Example value, replace with actual input
            float bpModifiers = 1.0f; // Example value, replace with actual input

            //check for skill type ( either "Focus" or "Spread" ) and create atk and def stat index accordingly, if type is neither then return ["0", "0", "0%", "0%"]
            int atkStatIndex; // Default to Atk
            int defStatIndex ; // Default to Def

            if (skillData[3] == "Focus")
            {
                atkStatIndex = 1; // Atk
                defStatIndex = 2; // Def
            }
            else if (skillData[3] == "Spread")
            {
                atkStatIndex = 3; // Spa
                defStatIndex = 4; // Spd
            }
            else
            {
                return new string[] { "0", "0", "0%", "0%" }; // Invalid skill type
            }

            // Get the stats for the attacker and defender based on the side
            int[] attStats = side == "Player" ? pStats : eStats;
            int[] defStats = side == "Player" ? eStats : pStats;
            // Get the attacking and defending stats based on the skill type
            int attackingStat = attStats[atkStatIndex]; // Atk or Spa based on skill type
            int defenseStat = defStats[defStatIndex]; // Def or Spd based on skill type
            // Get the level of the attacker from _playerData or _enemyData based on side
            int attackerLevel = Convert.ToInt32(side == "Player" ? _playerData[_playerIndex][5] : _enemyData[_enemyIndex][5]);
            // Get the base power of the skill from skillData
            int basePower = Convert.ToInt32(skillData[6]); // Power of the skill


            // Calculate "attack" value for the damage formula:
            //	Atk modifiers include abilities that explicitly modify this stat, stat buffs, then darkness/fear in that order. Same with Def. Each are ※ operators.
            var A = ((attackerLevel * 0.4 + 2) * Math.Floor(attackingStat * atkModifiers) * Math.Floor(basePower * bpModifiers) / (defenseStat * defModifiers) / 50) + 2;

            // Calculate the final damage value:
            //Do this twice, once for min roll and once for max roll.
            //DMG = A ※ [Critical Hit] ※ [RNG] ※ [Effectiveness] ※ [STAB] ※ [Weather] ※ [Attacker Item] ※ [Defender Item] ※ [Attacker Ability] ※ [Defender Ability] ※ [Screens]

            //getting the types for each puppet
            string[] playerPuppetTypes = _mainWindow.GetPuppetData(_playerData[_playerIndex][0], _playerData[_playerIndex][1]);
            string[] enemyPuppetTypes = _mainWindow.GetPuppetData(_enemyData[_enemyIndex][0], _enemyData[_enemyIndex][1]);

            // Get the skill type from skillData
            string skillType = skillData[2]; // Element of the skill

            // Calculate effectiveness based on the skill type and defender's types
            double effectiveness = TypeEffectiveness.GetAttackEffectiveness(skillType, enemyPuppetTypes[0], enemyPuppetTypes[1]);

            // Calculate STAB (Same Type Attack Bonus)
            double stab = 1.0; // Default STAB value
            if (playerPuppetTypes.Contains(skillType))
            {
                stab = 1.5; // STAB bonus if the skill type matches the attacker's type
            }

            // Calculate the final damage rolls
            var Min = Math.Floor(Math.Floor(Math.Floor(A * 0.86) * effectiveness) * stab); // Minimum damage roll (86%)
            var Max = Math.Floor(Math.Floor(Math.Floor(A * 1.00) * effectiveness) * stab); // Maximum damage roll (100%)

            //calculate the percentage of damage dealt for min and max rolls based on hp of the defender
            double minDamagePercentage = (Min / defStats[0]) * 100;
            double maxDamagePercentage = (Max / defStats[0]) * 100;

            // Return the results as a string array
            string[] results = new string[4];
            results[0] = Min.ToString(); // Minimum damage
            results[1] = Max.ToString(); // Maximum damage
            results[2] = minDamagePercentage.ToString("F2") + "%"; // Minimum damage percentage
            results[3] = maxDamagePercentage.ToString("F2") + "%"; // Maximum damage percentage
            return results;

        }

        private void Puppet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ValueTuple<int, string> tagData)
            {
                int index = tagData.Item1;
                string side = tagData.Item2;
                
                List<string[]> data = side == "Player" ? _playerData : _enemyData;
                if (index < 0 || index >= data.Count)
                {
                    return;
                }

                if (side == "Player")
                {
                    _playerIndex = index;
                }
                else if (side == "Enemy")
                {
                    _enemyIndex = index;
                }

                updateUI(index, side);
            }
        }
    }
}
