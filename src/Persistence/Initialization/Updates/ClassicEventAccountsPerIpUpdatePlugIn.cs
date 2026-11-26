// <copyright file="ClassicEventAccountsPerIpUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates the <see cref="GameLogic.MiniGames.EventAccountsPerIpPlugIn"/> on an existing classic (Season 1 content) database,
/// so that at most <see cref="ClassicPlugInDefaults.MaximumAccountsPerIpInEvents"/> account of one IP address takes part in the same event.
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicEventAccountsPerIpUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicEventAccountsPerIpUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("D4F9493B-09C1-46B5-BC66-0A34888E1857")]
public class ClassicEventAccountsPerIpUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: one account per IP address in events";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Activates the plugin which lets only one account of an IP address take part in the same event (Devil Square, Blood Castle, Team vs Team, ...), over all game servers. As soon as an account entered the event, another account of the same IP address can't enter it anymore. The limit can be changed in the plugin configuration. Only applies to the classic (Season 1 content) data.";

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
            ClassicPlugInDefaults.ActivateEventAccountsPerIp(gameConfiguration, context, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        }

        return ValueTask.CompletedTask;
    }
}
