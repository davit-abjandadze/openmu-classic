// <copyright file="AdminRoles.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.AdminAuth;

/// <summary>
/// The names of the <see cref="AdminRole"/>s, as they are stored and used in the claims.
/// </summary>
public static class AdminRoles
{
    /// <summary>
    /// Gets the role which is allowed to see the state of the servers and the game data, but can't change anything.
    /// </summary>
    public static string Viewer => nameof(AdminRole.Viewer);

    /// <summary>
    /// Gets the role which is additionally allowed to operate the servers, e.g. start and stop them,
    /// disconnect players and edit accounts.
    /// </summary>
    public static string Operator => nameof(AdminRole.Operator);

    /// <summary>
    /// Gets the role which is additionally allowed to change the game configuration, install updates,
    /// set the database up and manage the admin panel users.
    /// </summary>
    public static string Administrator => nameof(AdminRole.Administrator);

    /// <summary>
    /// Gets the role which is allowed to grant cash shop coins to accounts.
    /// </summary>
    /// <remarks>
    /// It's not one of the <see cref="AdminRole"/>s which build up on each other, so that it can be given
    /// alone, e.g. to the API key of a payment provider. An <see cref="Administrator"/> has it implicitly.
    /// </remarks>
    public static string CashShop => "CashShop";

    /// <summary>
    /// Gets the role which is allowed to create accounts and to see what a <see cref="Viewer"/> sees,
    /// e.g. for the API key of the registration form of a website.
    /// </summary>
    /// <remarks>
    /// It's not one of the <see cref="AdminRole"/>s which build up on each other, so that the key of a website can create
    /// accounts without being able to edit or ban them, to disconnect players or to stop servers like an <see cref="Operator"/>.
    /// It implies the <see cref="Viewer"/> role, because such a website usually shows the server status and the rankings as well.
    /// An <see cref="Operator"/> and an <see cref="Administrator"/> have it implicitly.
    /// </remarks>
    public static string Registration => "Registration";

    /// <summary>
    /// Gets all defined roles which build up on each other, from the least to the most privileged one.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = Enum.GetNames<AdminRole>();

    /// <summary>
    /// Gets the roles which are implied by the specified role, including the role itself.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The role itself and all roles which are implied by it.</returns>
    /// <remarks>
    /// The roles build up on each other, so an <see cref="Administrator"/> is implicitly
    /// an <see cref="Operator"/> and a <see cref="Viewer"/> as well, and has the <see cref="CashShop"/> role.
    /// An <see cref="Operator"/> has the <see cref="Registration"/> role, which itself implies the <see cref="Viewer"/> role.
    /// </remarks>
    public static IEnumerable<string> GetEffectiveRoles(string role)
    {
        if (string.Equals(role, CashShop, StringComparison.OrdinalIgnoreCase))
        {
            yield return CashShop;
            yield break;
        }

        if (string.Equals(role, Registration, StringComparison.OrdinalIgnoreCase))
        {
            yield return Registration;
            yield return Viewer;
            yield break;
        }

        if (!Enum.TryParse<AdminRole>(role, out var parsedRole) || !Enum.IsDefined(parsedRole))
        {
            yield break;
        }

        foreach (var candidate in Enum.GetValues<AdminRole>())
        {
            if (candidate <= parsedRole)
            {
                yield return candidate.ToString();
            }
        }

        if (parsedRole >= AdminRole.Operator)
        {
            yield return Registration;
        }

        if (parsedRole == AdminRole.Administrator)
        {
            yield return CashShop;
        }
    }
}
