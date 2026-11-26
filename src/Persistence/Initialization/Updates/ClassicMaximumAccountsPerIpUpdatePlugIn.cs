// <copyright file="ClassicMaximumAccountsPerIpUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates the <see cref="GameLogic.PlugIns.MaximumConnectionsPerIpPlugIn"/> on an existing classic (Season 1 content) database,
/// so that at most <see cref="ClassicPlugInDefaults.MaximumAccountsPerIp"/> accounts are logged in from one IP address,
/// including the accounts which play in the offline mode.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMaximumAccountsPerIpUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicMaximumAccountsPerIpUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("BA4C9CB8-85DE-4F06-9D8C-A3294729B193")]
public class ClassicMaximumAccountsPerIpUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: at most 2 accounts per IP address";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Activates the plugin which limits the accounts logged in at the same time from one IP address to 2, over all game servers. Accounts which play in the offline mode (/offlevel) count, too. The limit can be changed in the plugin configuration. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            ClassicPlugInDefaults.ActivateMaximumAccountsPerIp(gameConfiguration, context, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }

        return ValueTask.CompletedTask;
    }
}
