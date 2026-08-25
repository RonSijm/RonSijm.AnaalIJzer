using System.Globalization;
using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.Arse.Components;

public partial class App
{
	private int MinimumConfidenceFocusOrder(ApplicationInputDefinition input)
	{
		var result = FirstCommandOptionFocusOrder(input);

		return result;
	}

	private int MinimumSupportFocusOrder(ApplicationInputDefinition input)
	{
		var result = FirstCommandOptionFocusOrder(input) + 1;

		return result;
	}

	private int OutputFocusOrder(ApplicationOperationDefinition operation, ApplicationInputDefinition input)
	{
		var result = FirstCommandOptionFocusOrder(input);
		if (operation.Kind == ApplicationOperationKind.GenerateConfig && _generationStrategy == ConventionsStrategy)
		{
			result += 2;
		}

		return result;
	}

	private int RunFocusOrder(ApplicationOperationDefinition operation, ApplicationInputDefinition input)
	{
		var result = operation.Kind == ApplicationOperationKind.Inspect
			? FirstCommandOptionFocusOrder(input)
			: OutputFocusOrder(operation, input) + 1;

		return result;
	}

	private int ClearFocusOrder(ApplicationOperationDefinition operation, ApplicationInputDefinition input)
	{
		var result = RunFocusOrder(operation, input) + 1;

		return result;
	}

	private static int FirstCommandOptionFocusOrder(ApplicationInputDefinition input)
	{
		var result = IsMsBuildInput(input.Kind) ? 2 : 1;

		return result;
	}

	private ConfigurationGenerationOptions CreateGenerationOptions()
	{
		if (_generationStrategy == SnapshotStrategy)
		{
			return new();
		}

		if (_generationStrategy == HelpfulStrategy)
		{
			return new ConfigurationGenerationOptions
			{
				Strategy = ConfigurationGenerationStrategy.Helpful
			};
		}

		if (!double.TryParse(_minimumConfidence, NumberStyles.Float, CultureInfo.InvariantCulture, out var minimumConfidence))
		{
			throw new ApplicationOperationException("Minimum confidence must be a number from 0 to 1.");
		}

		if (!int.TryParse(_minimumSupport, NumberStyles.None, CultureInfo.InvariantCulture, out var minimumSupport))
		{
			throw new ApplicationOperationException("Minimum supporting callers must be a whole number.");
		}

		var result = new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Conventions,
			MinimumConfidence = minimumConfidence,
			MinimumSupport = minimumSupport
		};

		return result;
	}
}
