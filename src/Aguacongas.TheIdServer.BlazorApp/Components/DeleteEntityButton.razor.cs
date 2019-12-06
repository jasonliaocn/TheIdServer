﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Aguacongas.TheIdServer.BlazorApp.Components
{
    public partial class DeleteEntityButton
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "This field is binded to a component.")]
        private string _checkEntityId;

        [Parameter]
        public string EntityId { get; set; }

        [Parameter]
        public EventCallback<string> DeleteConfirmed { get; set; }

        private async Task OnDeleteClicked()
        {
            if (_checkEntityId == EntityId)
            {
                await _jsRuntime.InvokeVoidAsync("bootstrapInteropt.dismissModal", "delete-entity");
                await DeleteConfirmed.InvokeAsync(EntityId)
                    .ConfigureAwait(false);
            }
        }

    }
}
