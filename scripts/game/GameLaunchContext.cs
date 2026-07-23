namespace AshwoodCounty3DPrototype.Game;

public static class GameLaunchContext
{
	private static bool _continueRequested;

	public static void RequestNewGame()
	{
		_continueRequested = false;
	}

	public static void RequestContinue()
	{
		_continueRequested = true;
	}

	public static bool ConsumeContinueRequest()
	{
		bool requested = _continueRequested;
		_continueRequested = false;
		return requested;
	}
}
