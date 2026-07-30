// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

// Exposes internal validation helpers (e.g. EnsureExtensions) to the test project for unit testing.
[assembly: InternalsVisibleTo("SnowflakeV2CoreLogic.Tests")]
