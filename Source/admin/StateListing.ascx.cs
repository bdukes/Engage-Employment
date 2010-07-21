// <copyright file="StateListing.ascx.cs" company="Engage Software">
// Engage: Employment
// Copyright (c) 2004-2010
// by Engage Software ( http://www.engagesoftware.com )
// </copyright>

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System; 
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Utilities;

namespace Engage.Dnn.Employment.Admin
{
    partial class StateListing : ModuleBase
    {
        private const int StateNameMaxLength = 255;
        private const int AbbreviationMaxLength = 10;

        protected static string MaxLengthValidationExpression
        {
            get { return Utility.GetMaxLengthValidationExpression(StateNameMaxLength); }
        }

        protected string MaxLengthValidationText
        {
            get { return String.Format(CultureInfo.CurrentCulture, Localization.GetString("StateMaxLength", LocalResourceFile), StateNameMaxLength); }
        }

        protected static string MaxAbbreviationLengthValidationExpression
        {
            get { return Utility.GetMaxLengthValidationExpression(AbbreviationMaxLength); }
        }

        protected string MaxAbbreviationLengthValidationText
        {
            get { return String.Format(CultureInfo.CurrentCulture, Localization.GetString("AbbreviationMaxLength", LocalResourceFile), AbbreviationMaxLength); }
        }

        protected override void OnInit(EventArgs e)
        {
            this.Load += Page_Load;
            this.AddButton.Click += this.AddButton_Click;
            this.BackButton.Click += this.BackButton_Click;
            this.CancelNewButton.Click += this.CancelNewButton_Click;
            this.SaveNewButton.Click += this.SaveNewButton_Click;
            this.StatesGridView.RowCancelingEdit += this.StatesGridView_RowCancelingEdit;
            this.StatesGridView.RowCommand += this.StatesGridView_RowCommand;
            this.StatesGridView.RowDataBound += this.StatesGridView_RowDataBound;
            this.StatesGridView.RowDeleting += this.StatesGridView_RowDeleting;
            this.StatesGridView.RowEditing += this.StatesGridView_RowEditing;
            
            base.OnInit(e);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected void Page_Load(Object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    Engage.Dnn.Utility.LocalizeGridView(ref this.StatesGridView, LocalResourceFile);
                    SetupLengthValidation();
                    LoadStates();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        protected void BackButton_Click(object sender, EventArgs e)
        {
            Response.Redirect(EditUrl(ControlKey.Edit.ToString()));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void AddButton_Click(object sender, EventArgs e)
        {
            this.NewPanel.Visible = true;
            txtNewState.Focus();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void SaveNewButton_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                if (IsStateNameUnique(null, txtNewState.Text))
                {
                    State.InsertState(txtNewState.Text, txtNewAbbreviation.Text, PortalId);
                    HideAndClearNewStatePanel();
                    LoadStates();
                }
                else
                {
                    cvDuplicateState.IsValid = false;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void CancelNewButton_Click(object sender, EventArgs e)
        {
            HideAndClearNewStatePanel();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void StatesGridView_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            this.StatesGridView.EditIndex = -1;
            LoadStates();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void StatesGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                GridViewRow row = e.Row;
                if (row != null)
                {
                    Button btnDelete = (Button)row.FindControl("btnDelete");
                    if (btnDelete != null)
                    {
                        int? stateId = GetStateId(row);
                        if (stateId.HasValue && State.IsStateUsed(stateId.Value))
                        {
                            btnDelete.Enabled = false;
                        }
                        else
                        {
                            btnDelete.OnClientClick = string.Format(CultureInfo.CurrentCulture, "return confirm('{0}');", ClientAPI.GetSafeJSString(Localization.GetString("DeleteConfirm", LocalResourceFile)));
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void StatesGridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int? stateId = GetStateId(e.RowIndex);
            if (stateId.HasValue)
            {
                State.DeleteState(stateId.Value);
                LoadStates();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void StatesGridView_RowEditing(object sender, GridViewEditEventArgs e)
        {
            this.StatesGridView.EditIndex = e.NewEditIndex;
            HideAndClearNewStatePanel();
            LoadStates();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
        private void StatesGridView_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (string.Equals("Save", e.CommandName, StringComparison.OrdinalIgnoreCase))
            {
                if (Page.IsValid)
                {
                    int rowIndex;
                    if (int.TryParse(e.CommandArgument.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rowIndex))
                    {
                        int? stateId = GetStateId(rowIndex);
                        if (stateId.HasValue)
                        {
                            string newStateName = GetStateName(rowIndex);
                            if (IsStateNameUnique(stateId, newStateName))
                            {
                                State.UpdateState(stateId.Value, newStateName, GetStateAbbreviation(rowIndex));
                                this.StatesGridView.EditIndex = -1;
                                LoadStates();
                            }
                            else
                            {
                                cvDuplicateState.IsValid = false;
                            }
                        }
                    }
                }
            }
        }

        private bool IsStateNameUnique(int? stateId, string newStateName)
        {
            int? newStateId = State.GetStateId(newStateName, PortalId);
            return !newStateId.HasValue || (stateId.HasValue && newStateId.Value == stateId.Value);
        }

        private void LoadStates()
        {
            List<State> states = State.LoadStates(null, PortalId);
            this.StatesGridView.DataSource = states;
            this.StatesGridView.DataBind();

            if (states == null || states.Count % 2 == 0)
            {
                this.NewPanel.CssClass = this.StatesGridView.RowStyle.CssClass;
            }
            else
            {
                this.NewPanel.CssClass = this.StatesGridView.AlternatingRowStyle.CssClass;
            }

            rowNewHeader.Visible = (states == null || states.Count < 1);
        }

        private void SetupLengthValidation()
        {
            regexNewState.ValidationExpression = MaxLengthValidationExpression;
            regexNewState.ErrorMessage = MaxLengthValidationText;
            regexNewAbbreviation.ValidationExpression = MaxAbbreviationLengthValidationExpression;
            regexNewAbbreviation.ErrorMessage = MaxAbbreviationLengthValidationText;
        }

        private void HideAndClearNewStatePanel()
        {
            this.NewPanel.Visible = false;
            this.txtNewState.Text = string.Empty;
            this.txtNewAbbreviation.Text = string.Empty;
        }

        private string GetStateName(int rowIndex)
        {
            if (this.StatesGridView != null && this.StatesGridView.Rows.Count > rowIndex)
            {
                GridViewRow row = this.StatesGridView.Rows[rowIndex];
                TextBox txtState = row.FindControl("txtState") as TextBox;
                Debug.Assert(txtState != null);
                return txtState.Text;
            }
            return null;
        }

        private string GetStateAbbreviation(int rowIndex)
        {
            if (this.StatesGridView != null && this.StatesGridView.Rows.Count > rowIndex)
            {
                GridViewRow row = this.StatesGridView.Rows[rowIndex];
                TextBox txtAbbreviation = row.FindControl("txtAbbreviation") as TextBox;
                Debug.Assert(txtAbbreviation != null);
                return txtAbbreviation.Text;
            }
            return null;
        }

        private static int? GetStateId(Control row)
        {
            HiddenField hdnStateId = (HiddenField)row.FindControl("hdnStateId");

            int stateId;
            if (hdnStateId != null && int.TryParse(hdnStateId.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out stateId))
            {
                return stateId;
            }
            return null;
        }

        private int? GetStateId(int rowIndex)
        {
            if (this.StatesGridView != null && this.StatesGridView.Rows.Count > rowIndex)
            {
                return GetStateId(this.StatesGridView.Rows[rowIndex]);
            }
            return null;
        }

    }
}