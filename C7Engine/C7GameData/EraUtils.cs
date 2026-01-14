namespace C7GameData;

public static class EraUtils {

	// The civilopedia (CVLPD) version for each of the era names
	public const string ANCIENT_TIMES_CVLPD = "ERAS_Ancient_Times";
	public const string MIDDLE_AGES_CVLPD = "ERAS_Middle_Ages";
	public const string INDUSTRIAL_AGE_CVLPD = "ERAS_Industrial_Age";
	public const string MODERN_ERA_CVLPD = "ERAS_Modern_Era";

	public const string ANCIENT_TIMES = "Ancient Times";
	public const string MIDDLE_AGES = "Middle Ages";
	public const string INDUSTRIAL_AGE = "Industrial Age";
	public const string MODERN_ERA = "Modern Era";

	public static int GetEraIndex(string era) {
		if (era == ANCIENT_TIMES_CVLPD) {
			return 0;
		} else if (era == MIDDLE_AGES_CVLPD) {
			return 1;
		} else if (era == INDUSTRIAL_AGE_CVLPD) {
			return 2;
		} else if (era == MODERN_ERA_CVLPD) {
			return 3;
		}
		return -1;
	}

	public static string EraIndexToEra(int index) {
		if (index <= 0) {
			return ANCIENT_TIMES_CVLPD;
		} else if (index == 1) {
			return MIDDLE_AGES_CVLPD;
		} else if (index == 2) {
			return INDUSTRIAL_AGE_CVLPD;
		} else {
			return MODERN_ERA_CVLPD;
		}
	}

	public static string GetNiceEraName(string era) {
		if (era == ANCIENT_TIMES_CVLPD) {
			return ANCIENT_TIMES;
		} else if (era == MIDDLE_AGES_CVLPD) {
			return MIDDLE_AGES;
		} else if (era == INDUSTRIAL_AGE_CVLPD) {
			return INDUSTRIAL_AGE;
		} else if (era == MODERN_ERA_CVLPD) {
			return MODERN_ERA;
		}
		return $"Not A Valid Era: {era}";
	}

	public static string GetNextEraNiceName(string era) {
		if (era == ANCIENT_TIMES_CVLPD) {
			return MIDDLE_AGES;
		} else if (era == MIDDLE_AGES_CVLPD) {
			return INDUSTRIAL_AGE;
		}
		return MODERN_ERA;
	}

	public static string GetPreviousEraNiceName(string era) {
		if (era == MODERN_ERA_CVLPD) {
			return INDUSTRIAL_AGE;
		} else if (era == INDUSTRIAL_AGE_CVLPD) {
			return MIDDLE_AGES;
		}
		return ANCIENT_TIMES;
	}

	public static string GetNextEraNameByIndex(int index) {
		return EraIndexToEra(index + 1);
	}
	public static string GetPreviousEraNameByIndex(int index) {
		return EraIndexToEra(index - 1);
	}
}
