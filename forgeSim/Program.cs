using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
/**
Guild Wars 2 Mystic Forge simulator.
Simple POC. Proves the Wiki formula wrong, if you're willing to spend quite some time ingame gathering data.
**/
namespace forgeSim
{
    class Program
    {
		//simple POC setup: dumpItemsAndRecipes is only really needed once.
		//recipeSkimmer is an optional component of the above. It removes impossible results.
		//splitRecipeDB splits the db by item type. 
        static void Main(string[] args)
        {
            //            dumpItemsAndRecipes();
            //            recipeSkimmer();
             //splitRecipeDB();
			//searchDB currently hardcoded, but it would be trivial to do Convert.ToInt32(args[0]), Convert.ToInt32(args[1]);
            searchDB(13454, 15);
        }

        //Takes an ID and a item type.
		//Searches the database for every possible outcome from the Mystic Forge according to the Wiki's information.
		
        private static void searchDB(int i, int j)
        {
            StreamReader str = new StreamReader("recipes" + j + ".json");
            JsonTextReader jsr = new JsonTextReader(str);
            JArray jar = JArray.Load(jsr);
            str.Close();

            StreamReader strarr = new StreamReader("itemDumpType" + j + ".json");
            JsonTextReader jsrarr = new JsonTextReader(strarr);
            JArray queryarr = JArray.Load(jsrarr);
            foreach(JObject jresult in jar)
            {
                if((int)jresult["result_item_data_id"] == i)
                {
                    Console.WriteLine("Result found: " + (string)jresult["name"]);
                    foreach(JObject jR in queryarr)
                    {
                        int levelDiff = (int)jR["restriction_level"] - (int)jresult["restriction_level"];
                        int rarityDiff = (int)jR["rarity"] - (int)jR["rarity"];
                        if ((int)jR["sub_type_id"] == (int)jresult["sub_type_id"] && levelDiff >= 3 && levelDiff <= 12)
                        {
                            Console.WriteLine("Candidate: " + jR["name"] + " with value of :" + jR["min_sale_unit_price"]);
                            Console.WriteLine("Level difference: " + levelDiff);
                        }
                    }
                }
            }

            Console.ReadLine();
        }

        //This is the ugliest thing I've ever written
        private static void splitRecipeDB()
        {
            //Read recipesParsed.json. I did not check if it exists. Jesus.
            StreamReader str = new StreamReader("recipesParsed.json");
            //JsonTextReaders have to use a StreamReader. 
            JsonTextReader jsr = new JsonTextReader(str);

            //JsonTextWriters similarly have to use a StreamWriter. 
            //We need a JArray for each outcome.
            StreamWriter stwrite0 = new StreamWriter("recipes0.json");
            JsonTextWriter jstrwrite0 = new JsonTextWriter(stwrite0);
            JArray jar0 = new JArray();

            //Mother of god I couldn't come up with a better way.
            StreamWriter stwrite15 = new StreamWriter("recipes15.json");
            JsonTextWriter jstrwrite15 = new JsonTextWriter(stwrite15);
            JArray jar15 = new JArray();

            //I'm so sorry.
            StreamWriter stwrite18 = new StreamWriter("recipes18.json");
            JsonTextWriter jstrwrite18 = new JsonTextWriter(stwrite18);
            JArray jar18 = new JArray();
            
            //Load the entire file at once.
            JArray completeRecipes = JArray.Load(jsr);
            //For each object we can parse, sort it into one of the three arrays.
            foreach (JObject j in completeRecipes)
            {
                if((string)j["type_id"] == "0")
                {
                    jar0.Add(j);
                }
                else if((string)j["type_id"] == "15")
                {
                    jar15.Add(j);
                }
                else if((string)j["type_id"] == "18")
                {
                    jar18.Add(j);
                }
            }
            //Write all three arrays.
            jar0.WriteTo(jstrwrite0);
            jar15.WriteTo(jstrwrite15);
            jar18.WriteTo(jstrwrite18);
            jstrwrite0.Close();
            jstrwrite15.Close();
            jstrwrite18.Close();
        }


        //Improves information in recipe file by removing irrelevant entries and gathering more itemdata from the gw2spidy API.
        private static void recipeSkimmer()
        {
            //reads recipes from file
            StreamReader str = new StreamReader("recipeJson.json");
            JsonTextReader jsr = new JsonTextReader(str);
            JArray jar = JArray.Load(jsr);
            //output array
            JArray output = new JArray();
            WebClient webClient = new WebClient();
            foreach(JObject j in jar)
            {
                Console.WriteLine("Getting item: " + (string)j["result_item_data_id"]);
                JObject compareItem = JObject.Parse(webClient.DownloadString("http://www.gw2spidy.com/api/v0.9/json/item/" + (string)j["result_item_data_id"]));
                if((int)compareItem["result"]["type_id"] == 0 || (int)compareItem["result"]["type_id"] == 15 || (int)compareItem["result"]["type_id"] == 18)
                {
                    Console.WriteLine("Valid item found: Type:" + compareItem["result"]["type_id"] + " Name: " + compareItem["result"]["name"]);
                    j.Merge(compareItem["result"], new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });

                    output.Add(j);
                }
            }

            using (StreamWriter swRecipeOutput = File.CreateText("recipesParsed.json"))
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(swRecipeOutput))
                {
                    output.WriteTo(jsonWriter);
                }
            }

        }
        //Dumps the items and recipes using the gw2spidy database
        static void dumpItemsAndRecipes()
        {
            //Weapons, trinkets, armor.
            int[] typesToDump = { 0, 15, 18 };
            WebClient webClient = new WebClient();

            //We need to dump these separately. Only these three item types are relevant.
            foreach (int i in typesToDump)
            {
                Console.WriteLine("Dumping database: " + i);
                JObject jAr = JObject.Parse(webClient.DownloadString("http://www.gw2spidy.com/api/v0.9/json/all-items/" + i));
                JArray jarray = new JArray();
                foreach(JObject j in jAr["results"])
                {
                    jarray.Add(j);
                }
                using (StreamWriter swRecipeOutput = File.CreateText("itemDumpType" + i  + ".json"))
                {
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(swRecipeOutput))
                    {
                        jarray.WriteTo(jsonWriter);
                    }
                }

            }

			//UNUSED RECIPE CODE 
            /*
            //This is really, really awkward. The first page of the database has the count of number of pages, though, so we need it to parse the rest 
            //correctly. I'm not sure why the gw2spidy database isn't treating it like all-items.
            JObject recipeBasePage = JObject.Parse(webClient.DownloadString("http://www.gw2spidy.com/api/v0.9/json/recipes/*all*"));
            
            //We need pages 1 through 91, as of this writing. Hardcoding this would be bad because the database will grow.
            JArray recipeArray = new JArray();
            for(int i = 1; i <= (int)recipeBasePage["last_page"]; i++)
            {
                JObject toIndex = JObject.Parse(webClient.DownloadString("http://www.gw2spidy.com/api/v0.9/json/recipes/*all*" + i));
                foreach(JObject v in toIndex["results"])
                {
                    recipeArray.Add(v);
                }
            }
            using(StreamWriter swRecipeOutput = File.CreateText("recipeJson.json"))
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(swRecipeOutput))
                    {
                        recipeArray.WriteTo(jsonWriter);
                    }
            }
            */
        }
    }
}
