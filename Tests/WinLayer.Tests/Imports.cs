global using Avalonia.Controls;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Interactivity;
global using Avalonia.Threading;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;
global using Microsoft.Win32.SafeHandles;
global using System.Buffers.Binary;
global using System.Collections.Concurrent;
global using System.Diagnostics;
global using System.IO.Pipes;
global using System.Reflection;
global using System.Security.AccessControl;
global using System.Security.Principal;
global using WinLayer.Client;
global using WinLayer.Contracts;
global using WinLayer.Service;
global using Xunit;
global using Xunit.Sdk;
global using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]
