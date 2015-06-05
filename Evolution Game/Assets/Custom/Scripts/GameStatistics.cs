using UnityEngine;
using System.Collections;

public static class GameStatistics {

	private static int lives = 0;

	private static int eatenPlants = 0;
	private static int eatenRegularPlants = 0;

	private static int deaths = 0;
	private static int deathsByFish = 0;
	private static int deathsByTeethFish = 0;

	public static void addEatenPlant(PlantType plantType) {
		eatenPlants += 1;
		switch (plantType) {
		case PlantType.REGULAR_PLANT: 
			eatenRegularPlants += 1;
			break;
		}
	}

	public static void addDeathByFish(FishType fishType) {
		deaths += 1;
		deathsByFish += 1;
		switch (fishType) {
		case FishType.TEETH_FISH:
			deathsByTeethFish += 1;
			break;
		}
	}

	public static void incrementLives() {
		lives += 1;
		GUIManager.instance.setLivesText();
	}

	public static void decrementLives() {
		lives -= 1;
		GUIManager.instance.setLivesText();
	}

	public static int getAmountRegularPlants() {
		return eatenRegularPlants;
	}

	public static int getLives() {
		return lives;
	}
}
