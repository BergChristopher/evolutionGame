using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GameStatistics {

	private static int lives = 0;

	private static int eatenEatables = 0;
	private static Dictionary<EatableType, int> eatenEatablesByType = new Dictionary<EatableType, int>();
	//private static int eatenRegularPlants = 0;

	private static int deaths = 0;
	private static int deathsByFish = 0;
	private static Dictionary<FishType,int> deathsByFishType = new Dictionary<FishType, int>();
	
	public static void addEatable(EatableType eatableType) {
		eatenEatables += 1;
		if (eatenEatablesByType.ContainsKey(eatableType)) {
			eatenEatablesByType[eatableType] += 1;
		} else {
			eatenEatablesByType.Add(eatableType, 1);
		}
	}

	public static void addDeathByFish(FishType fishType) {
		deaths += 1;
		deathsByFish += 1;
		if (deathsByFishType.ContainsKey(fishType)) {
			deathsByFishType[fishType] += 1;
		} else {
			deathsByFishType.Add(fishType, 1);
		}
		GUIManager.instance.updateDeathsText();
	}

	public static void incrementLives() {
		lives += 1;
		GUIManager.instance.updateLivesText();
	}

	public static void decrementLives() {
		lives -= 1;
		GUIManager.instance.updateLivesText();
	}

	public static int getEatenRegularPlants() {
		int result = 0;
		if(eatenEatablesByType.ContainsKey(EatableType.REGULAR_PLANT)) {
			result = eatenEatablesByType[EatableType.REGULAR_PLANT];
		}
		return result;
	}

	public static int getLives() {
		return lives;
	}

	public static Dictionary<FishType, int> getDeathByFishType() {
		return deathsByFishType;
	}

	public static int getDeaths() {
		return deaths;
	}

	public static int getDeathsByFish() {
		return deathsByFish;
	}
}
