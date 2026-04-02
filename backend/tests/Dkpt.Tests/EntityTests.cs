using Dkpt.Application.Common.DTOs;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Enums;

namespace Dkpt.Tests;

public class EntityTests
{
    [Fact]
    public void NewMember_HasCorrectDefaults()
    {
        // Act
        var member = new Member();

        // Assert
        Assert.Equal(DateTime.UtcNow.Year, member.AnneeDebut);
        Assert.True(member.Actif);
        Assert.Empty(member.Prenom);
        Assert.Empty(member.Nom);
    }

    [Fact]
    public void NewPayment_DefaultPaymentMethod_IsEspeces()
    {
        // Act
        var payment = new Payment();

        // Assert
        Assert.Equal(PaymentMethod.Especes, payment.MoyenPaiement);
        Assert.Equal(0m, payment.Montant);
        Assert.Equal(0m, payment.FraisPaiement);
    }

    [Fact]
    public void NewUser_DefaultRole_IsLecteur()
    {
        // Act
        var user = new User();

        // Assert
        Assert.Equal(UserRole.Lecteur, user.Role);
        Assert.Empty(user.Email);
    }

    [Fact]
    public void PagedResult_StoresCorrectValues()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var result = new PagedResult<string>(items, 50, 1, 10);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }
}
