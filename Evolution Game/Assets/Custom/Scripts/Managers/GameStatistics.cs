using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GameStatistics {

	private static int lives = 0;

	private static int gatheredCollectables = 0;
	private static Dictionary<CollectableType, int> gatheredCollectablesByType = new Dictionary<CollectableType, int>();

	private static int gatheredRewards = 0;
	private static Dictionary<RewardType, int> gatheredRewardsByType = new Dictionary<RewardType, int>();

	private static int deaths = 0;
	private static int deathsByFish = 0;
	private static Dictionary<FishType,int> deathsByFishType = new Dictionary<FishType, int>();
	
	public static void addCollectable(CollectableType collectableType) {
		gatheredCollectables += 1;
		if (gatheredCollectablesByType.ContainsKey(collectableType)) {
			gatheredCollectablesByType[collectableType] += 1;
		} else {
			gatheredCollectablesByType.Add(collectableType, 1);
		}
		GUIManager.instance.updateCollectablesAndRewards();
	}

	public static void addReward(RewardType rewardType) {
		gatheredRewards += 1;
		if (gatheredRewardsByType.ContainsKey(rewardType)) {
			gatheredRewardsByType[rewardType] += 1;
		} else {
			gatheredRewardsByType.Add(rewardType, 1);
		}
		GUIManager.instance.updateCollectablesAndRewards();
	}

	public static void addDeathByFish(FishType fishType) {
		deaths += 1;
		deathsByFish += 1;
		if (deathsByFishType.ContainsKey(fishType)) {
			deathsByFishType[fishType] += 1;
		} else {
			deathsByFishType.Add(fishType, 1);
		}
		//GUIManager.instance.updateDeathsText();
	}

	public static void incrementLives() {
		lives += 1;
		GUIManager.instance.updateLivesText();
	}

	public static void decrementLives() {
		lives -= 1;
		GUIManager.instance.updateLivesText();
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

	public static Dictionary<CollectableType, int> getGatheredCollectables() {
		return gatheredCollectablesByType;
	}

	public static Dictionary<RewardType, int> getGatheredRewards() {
		return gatheredRewardsByType;
	}

	public static int getGatheredCollectablesOfType(CollectableType collectableType) {
		int result = 0;
		if(gatheredCollectablesByType.ContainsKey(collectableType)) {
			result = gatheredCollectablesByType[collectableType];
		}
		return result;
	}

	public static int getGatheredRewardsOfType(RewardType rewardType) {
		int result = 0;
		if(gatheredRewardsByType.ContainsKey(rewardType)) {
			result = gatheredRewardsByType[rewardType];
		}
		return result;
	}

	public static bool decrementGatheredRewardsOfType(RewardType rewardType, int amount) {
		bool success = false;
		if(gatheredRewardsByType.ContainsKey(rewardType) && gatheredRewardsByType[rewardType] >= amount ) {
			gatheredRewardsByType[rewardType] -= amount;
			success = true;
		}
		GUIManager.instance.updateCollectablesAndRewards();
		return success;
	}

	public static void clearRewardsAndCollectables() {
		gatheredRewards = 0;
		gatheredCollectables = 0;
		gatheredCollectablesByType.Clear();
		gatheredRewardsByType.Clear();
		GUIManager.instance.updateCollectablesAndRewards();
	}

	public static void clearAll() {
		Debug.Log("clearing all gamestats");
		lives = 0;
		deaths = 0;
		deathsByFish = 0;
		deathsByFishType.Clear();
		
		clearRewardsAndCollectables();
	}
}
