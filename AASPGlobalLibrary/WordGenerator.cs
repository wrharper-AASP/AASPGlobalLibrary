//using Newtonsoft.Json;

//Just a standard word generator
//kept old code in case something goes wrong with newer faster idea
//no longer needs the Newtonsoft library
namespace AASPGlobalLibrary
{
    public static class WordGenerator
    {
        /*public class JSONWordsList
        {
            public Listofword[]? Listofwords { get; set; }

            public class Listofword
            {
                public string? Word { get; set; }
            }
        }*/

        static dynamic? wordslist;
        public static string GetRandomWord()
        {
            //Newtonsoft.Json.JsonSerializer serializer = new();
            //JSONWordsList CreateRandomWordList = new();
            //using (StreamReader sr = new(Environment.CurrentDirectory + @"\JSONS\WordsList.json"))
            //byte[] bytes = File.ReadAllBytes(Environment.CurrentDirectory + @"\JSONS\WordsList.json");
            //{
            //using Newtonsoft.Json.JsonReader jr = new Newtonsoft.Json.JsonTextReader(sr);
            wordslist ??= Globals.DynamicBytesJsonDeserializer<dynamic>(Environment.CurrentDirectory + @"\JSONS\WordsList.json");// JsonSerializer.Deserialize<JSONWordsList>(bytes); //serializer.Deserialize<JSONWordsList>(jr);
            //}
            Random random = new();
            int randomWord = random.Next(0, wordslist.listofwords.Count);

            return wordslist.listofwords[randomWord].word;
        }
    }
}
