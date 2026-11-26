// <copyright file="ControlCenter.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.ItemExchange;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.TeamVsTeam;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Web.AdminPanel.Operations;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.Shared;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// A single page which shows the most important things of the server at once:
/// the servers, the main game settings, the features (plugins) and links to all detailed editors.
/// </summary>
/// <remarks>
/// The page doesn't implement any changes itself. It uses the shared operations
/// (<see cref="ConfigurationWriteOperations"/>, <see cref="ConfigurationTaskOperations"/>, <see cref="PlugInController"/>),
/// so every change is previewed, confirmed and audited like on the detailed pages.
/// </remarks>
public partial class ControlCenter : ComponentBase, IDisposable
{
    /// <summary>
    /// The feature groups. A plugin may be listed in more than one group, e.g. the Team vs Team is a PvP event
    /// and its settings hold its rewards; all groups then show the same item.
    /// </summary>
    private static readonly FeatureGroup[] FeatureGroups =
    [
        new(() => Resources.ControlCenterResets, [typeof(ResetFeaturePlugIn), typeof(ResetExperienceRatePlugIn), typeof(StatResetFeaturePlugIn)]),
        new(() => Resources.ControlCenterRewards, [typeof(HuntingJewelPlugIn), typeof(HuntingPointsPlugIn), typeof(ItemExchangeChatCommandPlugIn), typeof(MiniGameMedalsPlugIn), typeof(TeamVsTeamPlugIn)]),
        new(() => Resources.ControlCenterPvp, [typeof(PvpSpotsPlugIn), typeof(TeamVsTeamPlugIn)]),
        new(() => Resources.ControlCenterEvents, [typeof(DevilSquareStartPlugIn), typeof(BloodCastleStartPlugIn), typeof(GoldenInvasionPlugIn), typeof(RedDragonInvasionPlugIn), typeof(WhiteWizardInvasionPlugIn), typeof(EventAccountsPerIpPlugIn), typeof(GameLogic.CastleSiege.CastleSiegePlugIn)]),
        new(() => Resources.ControlCenterOther, [typeof(PartySoloExperiencePlugIn), typeof(ItemLevelLimitPlugIn), typeof(MapExcellentDropLevelsPlugIn), typeof(UnlockedCharacterClassesPlugIn), typeof(BossMapMarkersPlugIn), typeof(PeriodicSaveProgressPlugIn), typeof(MaximumConnectionsPerIpPlugIn)]),
    ];

    private IList<IManageableServer> _servers = [];
    private IDictionary<int, IGameServer>? _gameServers;
    private Guid _gameConfigurationId;
    private GameSettings? _settings;
    private GameSettings? _savedSettings;
    private bool _isMoneyRateAvailable;
    private List<ServerRate> _serverRates = [];
    private List<(FeatureGroup Group, List<PlugInConfigurationViewItem> Items)> _features = [];
    private bool _isSaving;

    [Inject]
    private IServerProvider ServerProvider { get; set; } = null!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> GameConfigurationSource { get; set; } = null!;

    [Inject]
    private ConfigurationWriteOperations WriteOperations { get; set; } = null!;

    [Inject]
    private ConfigurationTaskOperations TaskOperations { get; set; } = null!;

    [Inject]
    private PluginWriteOperations PluginOperations { get; set; } = null!;

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private IPersistenceContextProvider PersistenceContextProvider { get; set; } = null!;

    [Inject]
    private IModalService ModalService { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<ControlCenter> Logger { get; set; } = null!;

    private bool HasSettingChanges =>
        (this._settings is not null && this._savedSettings is not null && this._settings != this._savedSettings)
        || this._serverRates.Any(r => r.IsChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        this.ServerProvider.PropertyChanged -= this.OnServersChanged;
        this.PlugInController.DataChanged -= this.OnPlugInsChanged;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync().ConfigureAwait(true);
        this._servers = this.ServerProvider.Servers;
        this._gameServers = this.ServiceProvider.GetService<IDictionary<int, IGameServer>>();
        this.ServerProvider.PropertyChanged += this.OnServersChanged;
        this.PlugInController.DataChanged += this.OnPlugInsChanged;

        // The admin operations need the interactive circuit, so nothing is loaded while prerendering.
        if (!this.RendererInfo.IsInteractive)
        {
            return;
        }

        await this.LoadSettingsAsync().ConfigureAwait(true);
        await this.LoadFeaturesAsync().ConfigureAwait(true);
    }

    private static string Caption(string propertyName) => typeof(GameConfiguration).GetPropertyCaption(propertyName);

    private static string Format(float value) => value.ToString("0.###", CultureInfo.CurrentCulture);

    private static PlugInConfigurationViewItem CreateViewItem(Type plugInType, PlugInConfiguration configuration)
    {
        var display = plugInType.GetCustomAttribute<DisplayAttribute>();
        return new PlugInConfigurationViewItem(configuration)
        {
            Id = configuration.GetId(),
            TypeId = configuration.TypeId,
            TypeName = plugInType.FullName,
            PlugInName = display?.GetName() ?? plugInType.Name,
            PlugInDescription = display?.GetDescription(),
            IsActive = configuration.IsActive,
            ConfigurationType = plugInType.GetCustomConfigurationType(),
        };
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var configuration = await this.GameConfigurationSource.GetOwnerAsync().ConfigureAwait(true);
            this._gameConfigurationId = configuration.GetId();
            var rates = await this.TaskOperations.RatesGetAsync(default).ConfigureAwait(true);
            var moneyRates = rates["moneyAmount"] as JsonArray;
            this._isMoneyRateAvailable = moneyRates?.Count == 1;

            this._savedSettings = new GameSettings
            {
                ExperienceRate = configuration.ExperienceRate,
                MoneyRate = this._isMoneyRateAvailable ? moneyRates![0]!["value"]!.GetValue<float>() : 1,
                MaximumLevel = configuration.MaximumLevel,
                MaximumPartySize = configuration.MaximumPartySize,
                MaximumCharactersPerAccount = configuration.MaximumCharactersPerAccount,
                MaximumInventoryMoney = configuration.MaximumInventoryMoney,
                MaximumVaultMoney = configuration.MaximumVaultMoney,
                ItemDropDurationSeconds = (int)configuration.ItemDropDuration.TotalSeconds,
                MaximumItemOptionLevelDrop = configuration.MaximumItemOptionLevelDrop,
                ExcellentItemDropLevelDelta = configuration.ExcellentItemDropLevelDelta,
                ShouldDropMoney = configuration.ShouldDropMoney,
                AreaSkillHitsPlayer = configuration.AreaSkillHitsPlayer,
                PreventExperienceOverflow = configuration.PreventExperienceOverflow,
            };
            this._settings = this._savedSettings with { };

            this._serverRates = (rates["servers"] as JsonArray ?? [])
                .OfType<JsonObject>()
                .Select(server => new ServerRate(
                    server["serverId"]!.GetValue<int>(),
                    server["description"]?.GetValue<string>() ?? string.Empty,
                    server["experienceRate"]!.GetValue<float>()))
                .ToList();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Couldn't load the game settings: {Message}", ex.Message);
            this.ToastService.ShowError(string.Format(CultureInfo.CurrentCulture, Resources.UnexpectedErrorOccurred, ex.Message));
        }
    }

    /// <summary>
    /// Loads the features by their identifiers from a new context, because neither the shared game configuration of the
    /// data source nor the cached list of all plugin configurations is updated when a plugin gets activated or deactivated.
    /// </summary>
    private async Task LoadFeaturesAsync()
    {
        try
        {
            var gameConfiguration = await this.GameConfigurationSource.GetOwnerAsync().ConfigureAwait(true);
            using var context = this.PersistenceContextProvider.CreateNewTypedContext(typeof(PlugInConfiguration), false);
            var features = new List<(FeatureGroup Group, List<PlugInConfigurationViewItem> Items)>();

            // A plugin which is listed in several groups gets one item, so that a change shows in all of them.
            var viewItems = new Dictionary<Type, PlugInConfigurationViewItem>();
            foreach (var group in FeatureGroups)
            {
                var items = new List<PlugInConfigurationViewItem>();
                foreach (var plugInType in group.PlugInTypes)
                {
                    if (viewItems.TryGetValue(plugInType, out var viewItem))
                    {
                        items.Add(viewItem);
                    }
                    else if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == plugInType.GUID) is { } cached
                        && await context.GetByIdAsync<PlugInConfiguration>(cached.GetId()).ConfigureAwait(true) is { } persisted)
                    {
                        viewItem = CreateViewItem(plugInType, persisted);
                        viewItems.Add(plugInType, viewItem);
                        items.Add(viewItem);
                    }
                }

                if (items.Count > 0)
                {
                    features.Add((group, items));
                }
            }

            this._features = features;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Couldn't load the features: {Message}", ex.Message);
            this.ToastService.ShowError(string.Format(CultureInfo.CurrentCulture, Resources.UnexpectedErrorOccurred, ex.Message));
        }
    }

    private async Task OnToggleFeatureClickAsync(PlugInConfigurationViewItem item)
    {
        var activate = !item.IsActive;
        try
        {
            var preview = await this.PluginOperations.SetActiveAsync(item.TypeId, activate, false, null, default).ConfigureAwait(true);
            var question = string.Format(CultureInfo.CurrentCulture, activate ? Resources.ControlCenterActivateQuestion : Resources.ControlCenterDeactivateQuestion, item.PlugInName);
            if (!await this.ModalService.ShowQuestionAsync(item.PlugInName ?? string.Empty, question).ConfigureAwait(true))
            {
                return;
            }

            await this.PluginOperations.SetActiveAsync(item.TypeId, activate, true, preview["confirmationToken"]!.GetValue<string>(), default).ConfigureAwait(true);
            this.ToastService.ShowSuccess(Resources.SavedChanges);

            // Reloading in this circuit could still return the cached state, so the applied change is shown directly.
            item.IsActive = activate;
        }
        catch (AdminOperationException ex)
        {
            this.ToastService.ShowError(ex.Message);
        }
    }

    private void OnDiscardClick()
    {
        this._settings = this._savedSettings is null ? null : this._savedSettings with { };
        this._serverRates.ForEach(r => r.ExperienceRate = r.SavedExperienceRate);
    }

    private async Task OnSaveClickAsync()
    {
        if (this._settings is not { } settings || this._savedSettings is not { } saved)
        {
            return;
        }

        var changes = this.CollectChanges(settings, saved);
        if (changes.Count == 0)
        {
            this.ToastService.ShowSuccess(Resources.NoChangesToSave);
            return;
        }

        this._isSaving = true;
        try
        {
            var tokens = new List<string>(changes.Count);
            foreach (var change in changes)
            {
                var preview = await change.ExecuteAsync(false, null).ConfigureAwait(true);
                tokens.Add(preview["confirmationToken"]!.GetValue<string>());
            }

            var question = string.Join(" • ", changes.SelectMany(c => c.Descriptions));
            if (!await this.ModalService.ShowQuestionAsync(Resources.ControlCenterConfirmChanges, question).ConfigureAwait(true))
            {
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                await changes[i].ExecuteAsync(true, tokens[i]).ConfigureAwait(true);
            }

            this.ToastService.ShowSuccess(Resources.SavedChanges);
            await this.LoadSettingsAsync().ConfigureAwait(true);
        }
        catch (AdminOperationException ex)
        {
            this.ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "An unexpected error occurred on save: {Message}", ex.Message);
            this.ToastService.ShowError(string.Format(CultureInfo.CurrentCulture, Resources.UnexpectedErrorOccurred, ex.Message));
        }
        finally
        {
            this._isSaving = false;
        }
    }

    private List<SettingChange> CollectChanges(GameSettings settings, GameSettings saved)
    {
        var changes = new List<SettingChange>();
        var patch = new JsonObject();
        var descriptions = new List<string>();

        void Add<T>(string propertyName, T before, T after, Func<T, JsonNode?> toJson, Func<T, string> toText)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after))
            {
                patch[propertyName] = toJson(after);
                descriptions.Add($"{Caption(propertyName)}: {toText(before)} → {toText(after)}");
            }
        }

        static string Text<T>(T value) => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        Add(nameof(GameConfiguration.ExperienceRate), saved.ExperienceRate, settings.ExperienceRate, v => v, Format);
        Add(nameof(GameConfiguration.MaximumLevel), saved.MaximumLevel, settings.MaximumLevel, v => v, Text);
        Add(nameof(GameConfiguration.MaximumPartySize), saved.MaximumPartySize, settings.MaximumPartySize, v => v, Text);
        Add(nameof(GameConfiguration.MaximumCharactersPerAccount), saved.MaximumCharactersPerAccount, settings.MaximumCharactersPerAccount, v => v, Text);
        Add(nameof(GameConfiguration.MaximumInventoryMoney), saved.MaximumInventoryMoney, settings.MaximumInventoryMoney, v => v, v => v.ToString("N0", CultureInfo.CurrentCulture));
        Add(nameof(GameConfiguration.MaximumVaultMoney), saved.MaximumVaultMoney, settings.MaximumVaultMoney, v => v, v => v.ToString("N0", CultureInfo.CurrentCulture));
        Add(nameof(GameConfiguration.ItemDropDuration), saved.ItemDropDurationSeconds, settings.ItemDropDurationSeconds, v => TimeSpan.FromSeconds(v).ToString("c", CultureInfo.InvariantCulture), v => TimeSpan.FromSeconds(v).ToString("c", CultureInfo.InvariantCulture));
        Add(nameof(GameConfiguration.MaximumItemOptionLevelDrop), saved.MaximumItemOptionLevelDrop, settings.MaximumItemOptionLevelDrop, v => v, Text);
        Add(nameof(GameConfiguration.ExcellentItemDropLevelDelta), saved.ExcellentItemDropLevelDelta, settings.ExcellentItemDropLevelDelta, v => v, Text);
        Add(nameof(GameConfiguration.ShouldDropMoney), saved.ShouldDropMoney, settings.ShouldDropMoney, v => v, Text);
        Add(nameof(GameConfiguration.AreaSkillHitsPlayer), saved.AreaSkillHitsPlayer, settings.AreaSkillHitsPlayer, v => v, Text);
        Add(nameof(GameConfiguration.PreventExperienceOverflow), saved.PreventExperienceOverflow, settings.PreventExperienceOverflow, v => v, Text);

        if (patch.Count > 0)
        {
            var json = patch.ToJsonString();
            var id = this._gameConfigurationId;
            changes.Add(new SettingChange(descriptions, (apply, token) => this.WriteOperations.UpdateAsync(nameof(GameConfiguration), id, json, apply, token, default)));
        }

        if (this._isMoneyRateAvailable && settings.MoneyRate != saved.MoneyRate)
        {
            var value = settings.MoneyRate;
            changes.Add(new SettingChange(
                [$"{Resources.ControlCenterMoneyRate}: {Format(saved.MoneyRate)} → {Format(value)}"],
                (apply, token) => this.TaskOperations.RatesSetAsync("money", value, null, apply, token, default)));
        }

        foreach (var server in this._serverRates.Where(r => r.IsChanged))
        {
            var serverId = server.ServerId;
            var value = server.ExperienceRate;
            changes.Add(new SettingChange(
                [$"{server.Description} – {Caption(nameof(GameConfiguration.ExperienceRate))}: {Format(server.SavedExperienceRate)} → {Format(value)}"],
                (apply, token) => this.TaskOperations.RatesSetAsync("experience", value, serverId, apply, token, default)));
        }

        return changes;
    }

    private void OnServersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IServerProvider.Servers))
        {
            this._servers = this.ServerProvider.Servers;
            _ = this.InvokeAsync(this.StateHasChanged);
        }
    }

    private void OnPlugInsChanged(object? sender, EventArgs e)
    {
        _ = this.InvokeAsync(async () =>
        {
            await this.LoadFeaturesAsync().ConfigureAwait(true);
            this.StateHasChanged();
        });
    }

    /// <summary>
    /// A group of features, i.e. plugins, which belong together.
    /// </summary>
    /// <param name="GetCaption">Gets the localized caption of the group.</param>
    /// <param name="PlugInTypes">The types of the plugins of the group.</param>
    private sealed record FeatureGroup(Func<string> GetCaption, Type[] PlugInTypes);

    /// <summary>
    /// A previewable change of the settings.
    /// </summary>
    /// <param name="Descriptions">The descriptions of the changed values, which are shown for confirmation.</param>
    /// <param name="ExecuteAsync">Previews the change when its first argument is <c>false</c>,
    /// and applies it with the confirmation token of the preview when it's <c>true</c>.</param>
    private sealed record SettingChange(IReadOnlyList<string> Descriptions, Func<bool, string?, ValueTask<JsonObject>> ExecuteAsync);

    /// <summary>
    /// The edited main settings of the game configuration.
    /// </summary>
    private sealed record GameSettings
    {
        public float ExperienceRate { get; set; }

        public float MoneyRate { get; set; }

        public short MaximumLevel { get; set; }

        public byte MaximumPartySize { get; set; }

        public byte MaximumCharactersPerAccount { get; set; }

        public int MaximumInventoryMoney { get; set; }

        public int MaximumVaultMoney { get; set; }

        public int ItemDropDurationSeconds { get; set; }

        public byte MaximumItemOptionLevelDrop { get; set; }

        public byte ExcellentItemDropLevelDelta { get; set; }

        public bool ShouldDropMoney { get; set; }

        public bool AreaSkillHitsPlayer { get; set; }

        public bool PreventExperienceOverflow { get; set; }
    }

    /// <summary>
    /// The edited experience rate of a game server.
    /// </summary>
    private sealed class ServerRate
    {
        public ServerRate(int serverId, string description, float experienceRate)
        {
            this.ServerId = serverId;
            this.Description = description;
            this.ExperienceRate = experienceRate;
            this.SavedExperienceRate = experienceRate;
        }

        public int ServerId { get; }

        public string Description { get; }

        public float SavedExperienceRate { get; }

        public float ExperienceRate { get; set; }

        public bool IsChanged => this.ExperienceRate != this.SavedExperienceRate;
    }
}
