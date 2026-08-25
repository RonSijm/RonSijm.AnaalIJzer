using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Application;

internal static partial class ApplicationConfigurationGenerator
{
	private const string SchemaResourceName = "RonSijm.AnaalIJzer.Application.AnaalIJzer.xsd";
	private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
	private static readonly string[] CommonTypeSuffixes =
	[
		"Controller",
		"Endpoint",
		"Service",
		"Manager",
		"Coordinator",
		"Handler",
		"Repository",
		"Store",
		"Gateway",
		"Client",
		"Queryable",
		"Projection",
		"Factory",
		"Provider",
		"Validator",
		"Mapper",
		"Builder",
		"Options",
		"Configuration"
	];
}

