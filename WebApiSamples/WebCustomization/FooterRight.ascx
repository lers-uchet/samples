<%@ Control Language="C#" AutoEventWireup="true" ClassName="Lers.Web.Client.FooterRight" %>

<%@ Import Namespace="Lers.Web.Api" %>

<style type="text/css">
	#footer_right_userName
	{
		float: Right;
		padding-right: 10px;
	}
</style>

<div id="footer_right_userName">
	<asp:Label ID="userDisplayName" runat="server" Text="Label"></asp:Label>
</div>

<script runat="server">

protected void Page_PreRender(object sender, EventArgs e)
{
	this.userDisplayName.Text = User.DisplayName;
}

</script>
