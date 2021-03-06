﻿public enum FishType { 
	AGGRESSIVE_NON_INTERACTING, 
	NEUTRAL_FISH, 
	AGGRESSIVE_INTERACTING, 
	EATABLE_FISH,
	READY_TO_MATE_FISH
};

public enum MovementType { 
	NONE, 
	WAYPOINT_BASED, 
	HORIZONTAL, 
	FOLLOW_PLAYER, 
	GUARD_STARTING_SPOT, 
	FLEE_FROM_PLAYER, 
	RANDOM, 
	SWARM 
};

public enum CollectableType { 
	[Description("regular plant")]
	REGULAR_PLANT, 
	[Description("enemy fish egg")]
	ENEMY_FISH_EGG, 
	[Description("enemy fish")]
	ENEMY_FISH 
};

public static class CollectableTypeExtension {
	public static string getDescription(this CollectableType collectableType) {
		Description[] da = (Description[])(collectableType.GetType().GetField(collectableType.ToString())).GetCustomAttributes(typeof(Description), false);
		return da.Length > 0 ? da[0].Value : collectableType.ToString();
	}
}

public enum RewardType {
	NONE,
	STRENGTH,
	SPEED,
	LIBIDO
}

public static class RewardTypeExtension {
	public static string getDescription(this RewardType rewardType) {
		Description[] da = (Description[])(rewardType.GetType().GetField(rewardType.ToString())).GetCustomAttributes(typeof(Description), false);
		return da.Length > 0 ? da[0].Value : rewardType.ToString();
	}
}

public enum EventType {
	PLAYER_DEATH,
	GAME_OVER,
	GAME_WON,
	GAME_RESTARTED
}

public enum EmitterType {
	NONE,
	BUBBLES,
	HEARTS
}

public enum FishCategoryType {
	PLAYER_LIKE,
	HERING,
	SPEEDY,
	ZEBRA,
	YELLOW,
	SHARK,
	SWORDFISH,
	TEETH,
	CLOWN
}