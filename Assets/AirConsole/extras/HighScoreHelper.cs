using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

public class HighScoreHelper : MonoBehaviour {

	/// <summary>
	/// Converts a list of high scores into rank tables
	/// </summary>
	/// <param name="highscores">Highscores.</param>
	/// <returns>The returned object is a dictionary of the following format:
	/// 	{ 
	/// 		"world": {
	///				"title": "World",
	///				"highscores": [ ... array of high scores ...]
	///			},
	///			"country": {
	///				"title": "Switzerland",
	///				"highscores": [ ... array of high scores ...]
	///			},
	///			...
	///		} 
	/// </returns>
	public static JToken ConvertHighScoresToTables(JToken highscores) {

		JObject tables = new JObject ();

		for (int i = 0; i < highscores.Count(); ++i) {

			JObject highscore = (JObject)highscores[i];
			foreach (JProperty rankType in highscore["ranks"]){

				JObject rankTable;
				
				if (tables[rankType.Name] == null){

					rankTable = new JObject();

					if (rankType.Name == "friends") {
						rankTable.Add("title", "Friends");
					} else if (rankType.Name == "world") {
						rankTable.Add("title", "World");
					} else if (rankType.Name == "country" || rankType.Name == "region" || rankType.Name == "city") {
						rankTable.Add("title", highscore["location_" + rankType.Name + "_name"]);
					}

					tables[rankType.Name] = rankTable;
				} else {
					rankTable = (JObject)tables[rankType.Name];
				}

				if (rankTable["highscores"] == null){
					rankTable.Add("highscores", new JArray());
				}

				((JArray)rankTable["highscores"]).Add(highscore);
			}
		}

		return tables;

	}

	/// <summary>
	/// <para>Given a list of high scores, returns the best high scores for ghost mode. 
	/// Preference in order:</para>
	/// <para>- First, Ghosts with similar scores</para>
	/// <para>- Then, sort by connected players, friends, city, region, country, world</para>
	/// <para>If multiple Ghosts have the same preference score, random ones are chosen</para>
	/// </summary>
	/// <param name="highscores">Highscores.</param>
	/// <param name="count">How many ghosts you would like to get.</param>
	/// <param name="count">Optimal minimum score factor a ghost needs to have compared to the best score of a connected player. Default is 0.75f.</param>
	/// <param name="count">Optimal maximum score factor a ghost needs to have compared to the best score of a connected player. Default is 1.5f.</param>
	/// <returns>List of highscore entries that are best suited for ghost mode.</returns>
	public static JToken SelectBestGhosts(JToken highscores, int count, float minScoreFactor = 0.75f, float maxScoreFactor = 1.5f){
		float myScore = -1;
		List<JToken> candidates = new List<JToken>();

		for (int i = 0; i < highscores.Count(); ++i) {
			JObject highscore = (JObject)highscores [i];
			if (highscore["relationship"].ToString() == "requested"){
				if (myScore == -1 || myScore < (float)highscore["score"]) { 
					myScore = (float)highscore["score"];
				}
			}
			candidates.Add (highscore);
		}

		JObject preference = new JObject ();
		preference.Add("world", 1);
		preference.Add("country", 2);
		preference.Add("region", 3);
		preference.Add("city", 4);
		preference.Add("friends", 5);
		preference.Add("similar_score", 10);
		preference.Add("requested", 20);

		int randomUID = Mathf.FloorToInt(Random.value * 65536);
		int randomUIDChar = Mathf.FloorToInt(Random.value * 32);

		JArray result = new JArray ();
		foreach (JToken candidate in candidates.OrderBy (element => SortScore (element, preference, myScore, minScoreFactor, maxScoreFactor, randomUID, randomUIDChar))) {
			result.Add(candidate);
			if (result.Count() == count){
				break;
			}
		} 

		return result;

	}

	private static float SortScore(JToken highscore, JObject preference, float myScore, float minScoreFactor, float maxScoreFactor, int randomUID, int randomUIDChar){
		float sortScore = 0;
		foreach (JProperty rank in highscore["ranks"]) {
			sortScore = Mathf.Max (sortScore, (float)preference[rank.Name]);
		}

		if (myScore != -1) {

			if ((float)highscore ["score"] > myScore * minScoreFactor && (float)highscore ["score"] < myScore * maxScoreFactor) {
				sortScore += (int)preference ["similar_score"];
			}

			if (highscore ["relationship"].ToString () == "requested") {
				sortScore += (int)preference ["requested"];
			}

			string uid = highscore ["uids"] [randomUID % highscore["uids"].Count()].ToString();
			sortScore += 1.0f / (int)uid[randomUIDChar % uid.Length]; 

		}
		return -sortScore;
	}
}
