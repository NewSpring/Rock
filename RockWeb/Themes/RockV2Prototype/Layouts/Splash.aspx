﻿<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>
<!DOCTYPE html>

<script runat="server">

    // keep code below to call base class init method

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnInit( EventArgs e )
    {
        base.OnInit( e );

        lLogoSvg.Text = System.IO.File.ReadAllText( HttpContext.Current.Request.MapPath("~/Assets/Images/rock-logo-circle.svg") );
    }

</script>

<html>
<head runat="server">
    <meta charset="utf-8">
    <title></title>

    <!-- Set the viewport width to device width for mobile disabling zooming -->
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no">

    <script src="<%# System.Web.Optimization.Scripts.Url("~/Scripts/Bundles/RockJQueryLatest") %>"></script>

    <!-- Included CSS Files -->
    <link rel="stylesheet" href="<%# ResolveRockUrl("~~/Styles/bootstrap.css", true) %>"/>
	<link rel="stylesheet" href="<%# ResolveRockUrl("~~/Styles/theme.css", true) %>"/>

</head>
<body id="splash">

    <form id="form1" runat="server">

        <div id="content" class="login-form-container">
            <div class="logo-background">
                <div id="logo">
                    <asp:Literal ID="lLogoSvg" runat="server" />
                </div>
            </div>

            <div id="content-box" class="clearfix">
                <Rock:Zone Name="Main" runat="server" />
            </div>
        </div>

    </form>
</body>
</html>