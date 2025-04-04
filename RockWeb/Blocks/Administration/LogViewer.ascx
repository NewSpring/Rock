﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LogViewer.ascx.cs" Inherits="RockWeb.Blocks.Administration.LogViewer" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlLogs">
            <Rock:ModalAlert ID="mdAlert" runat="server" />
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-stream"></i>Logs</h1>
                    <asp:LinkButton ID="lbDownload" runat="server" CssClass="btn btn-default" Text="<i class='fa fa-download'></i>" ToolTip="Download Logs" OnClick="lbDownload_Click"></asp:LinkButton>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:Grid ID="rGrid" runat="server" EmptyDataText="No Logs Found" AllowCustomPaging="true" ShowPaginationText="false">
                            <Columns>
                                <Rock:RockBoundField DataField="DateTime" HeaderText="Date" />
                                <Rock:RockBoundField DataField="LogLevel" HeaderText="Level" />
                                <Rock:RockBoundField DataField="Category" HeaderText="Category" />
                                <Rock:RockBoundField DataField="Message" HeaderText="Message" />
                                <Rock:RockBoundField DataField="SerializedException" HeaderText="Exception" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
