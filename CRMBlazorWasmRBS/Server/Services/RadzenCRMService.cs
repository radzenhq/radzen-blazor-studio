using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Radzen;

using CRMBlazorWasmRBS.Server.Data;

namespace CRMBlazorWasmRBS.Server
{
    public partial class RadzenCRMService
    {
        RadzenCRMContext Context
        {
           get
           {
             return this.context;
           }
        }

        private readonly RadzenCRMContext context;
        private readonly NavigationManager navigationManager;

        public RadzenCRMService(RadzenCRMContext context, NavigationManager navigationManager)
        {
            this.context = context;
            this.navigationManager = navigationManager;
        }

        public void Reset() => Context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);


        public async Task ExportContactsToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/contacts/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/contacts/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportContactsToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/contacts/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/contacts/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnContactsRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact>> GetContacts(Query query = null)
        {
            var items = Context.Contacts.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnContactsRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnContactGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> GetContactById(int id)
        {
            var items = Context.Contacts
                              .AsNoTracking()
                              .Where(i => i.Id == id);

  
            var itemToReturn = items.FirstOrDefault();

            OnContactGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnContactCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);
        partial void OnAfterContactCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> CreateContact(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact contact)
        {
            OnContactCreated(contact);

            var existingItem = Context.Contacts
                              .Where(i => i.Id == contact.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.Contacts.Add(contact);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(contact).State = EntityState.Detached;
                throw;
            }

            OnAfterContactCreated(contact);

            return contact;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> CancelContactChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnContactUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);
        partial void OnAfterContactUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> UpdateContact(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact contact)
        {
            OnContactUpdated(contact);

            var itemToUpdate = Context.Contacts
                              .Where(i => i.Id == contact.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(contact);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterContactUpdated(contact);

            return contact;
        }

        partial void OnContactDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);
        partial void OnAfterContactDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Contact> DeleteContact(int id)
        {
            var itemToDelete = Context.Contacts
                              .Where(i => i.Id == id)
                              .Include(i => i.Opportunities)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnContactDeleted(itemToDelete);


            Context.Contacts.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterContactDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportOpportunitiesToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/opportunities/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/opportunities/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportOpportunitiesToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/opportunities/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/opportunities/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnOpportunitiesRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity>> GetOpportunities(Query query = null)
        {
            var items = Context.Opportunities.AsQueryable();

            items = items.Include(i => i.Contact);
            items = items.Include(i => i.OpportunityStatus);

            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnOpportunitiesRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnOpportunityGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> GetOpportunityById(int id)
        {
            var items = Context.Opportunities
                              .AsNoTracking()
                              .Where(i => i.Id == id);

            items = items.Include(i => i.Contact);
            items = items.Include(i => i.OpportunityStatus);
  
            var itemToReturn = items.FirstOrDefault();

            OnOpportunityGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnOpportunityCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);
        partial void OnAfterOpportunityCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> CreateOpportunity(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity opportunity)
        {
            OnOpportunityCreated(opportunity);

            var existingItem = Context.Opportunities
                              .Where(i => i.Id == opportunity.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.Opportunities.Add(opportunity);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(opportunity).State = EntityState.Detached;
                throw;
            }

            OnAfterOpportunityCreated(opportunity);

            return opportunity;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> CancelOpportunityChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnOpportunityUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);
        partial void OnAfterOpportunityUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> UpdateOpportunity(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity opportunity)
        {
            OnOpportunityUpdated(opportunity);

            var itemToUpdate = Context.Opportunities
                              .Where(i => i.Id == opportunity.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(opportunity);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterOpportunityUpdated(opportunity);

            return opportunity;
        }

        partial void OnOpportunityDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);
        partial void OnAfterOpportunityDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Opportunity> DeleteOpportunity(int id)
        {
            var itemToDelete = Context.Opportunities
                              .Where(i => i.Id == id)
                              .Include(i => i.Tasks)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnOpportunityDeleted(itemToDelete);


            Context.Opportunities.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterOpportunityDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportOpportunityStatusesToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/opportunitystatuses/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/opportunitystatuses/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportOpportunityStatusesToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/opportunitystatuses/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/opportunitystatuses/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnOpportunityStatusesRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus>> GetOpportunityStatuses(Query query = null)
        {
            var items = Context.OpportunityStatuses.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnOpportunityStatusesRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnOpportunityStatusGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> GetOpportunityStatusById(int id)
        {
            var items = Context.OpportunityStatuses
                              .AsNoTracking()
                              .Where(i => i.Id == id);

  
            var itemToReturn = items.FirstOrDefault();

            OnOpportunityStatusGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnOpportunityStatusCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);
        partial void OnAfterOpportunityStatusCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> CreateOpportunityStatus(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus opportunitystatus)
        {
            OnOpportunityStatusCreated(opportunitystatus);

            var existingItem = Context.OpportunityStatuses
                              .Where(i => i.Id == opportunitystatus.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.OpportunityStatuses.Add(opportunitystatus);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(opportunitystatus).State = EntityState.Detached;
                throw;
            }

            OnAfterOpportunityStatusCreated(opportunitystatus);

            return opportunitystatus;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> CancelOpportunityStatusChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnOpportunityStatusUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);
        partial void OnAfterOpportunityStatusUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> UpdateOpportunityStatus(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus opportunitystatus)
        {
            OnOpportunityStatusUpdated(opportunitystatus);

            var itemToUpdate = Context.OpportunityStatuses
                              .Where(i => i.Id == opportunitystatus.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(opportunitystatus);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterOpportunityStatusUpdated(opportunitystatus);

            return opportunitystatus;
        }

        partial void OnOpportunityStatusDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);
        partial void OnAfterOpportunityStatusDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.OpportunityStatus> DeleteOpportunityStatus(int id)
        {
            var itemToDelete = Context.OpportunityStatuses
                              .Where(i => i.Id == id)
                              .Include(i => i.Opportunities)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnOpportunityStatusDeleted(itemToDelete);


            Context.OpportunityStatuses.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterOpportunityStatusDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportTasksToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/tasks/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/tasks/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportTasksToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/tasks/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/tasks/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnTasksRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task>> GetTasks(Query query = null)
        {
            var items = Context.Tasks.AsQueryable();

            items = items.Include(i => i.Opportunity);
            items = items.Include(i => i.TaskStatus);
            items = items.Include(i => i.TaskType);

            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnTasksRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnTaskGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> GetTaskById(int id)
        {
            var items = Context.Tasks
                              .AsNoTracking()
                              .Where(i => i.Id == id);

            items = items.Include(i => i.Opportunity);
            items = items.Include(i => i.TaskStatus);
            items = items.Include(i => i.TaskType);
  
            var itemToReturn = items.FirstOrDefault();

            OnTaskGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnTaskCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);
        partial void OnAfterTaskCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> CreateTask(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task task)
        {
            OnTaskCreated(task);

            var existingItem = Context.Tasks
                              .Where(i => i.Id == task.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.Tasks.Add(task);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(task).State = EntityState.Detached;
                throw;
            }

            OnAfterTaskCreated(task);

            return task;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> CancelTaskChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnTaskUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);
        partial void OnAfterTaskUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> UpdateTask(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task task)
        {
            OnTaskUpdated(task);

            var itemToUpdate = Context.Tasks
                              .Where(i => i.Id == task.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(task);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterTaskUpdated(task);

            return task;
        }

        partial void OnTaskDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);
        partial void OnAfterTaskDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.Task> DeleteTask(int id)
        {
            var itemToDelete = Context.Tasks
                              .Where(i => i.Id == id)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnTaskDeleted(itemToDelete);


            Context.Tasks.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterTaskDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportTaskStatusesToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/taskstatuses/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/taskstatuses/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportTaskStatusesToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/taskstatuses/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/taskstatuses/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnTaskStatusesRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus>> GetTaskStatuses(Query query = null)
        {
            var items = Context.TaskStatuses.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnTaskStatusesRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnTaskStatusGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> GetTaskStatusById(int id)
        {
            var items = Context.TaskStatuses
                              .AsNoTracking()
                              .Where(i => i.Id == id);

  
            var itemToReturn = items.FirstOrDefault();

            OnTaskStatusGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnTaskStatusCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);
        partial void OnAfterTaskStatusCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> CreateTaskStatus(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus taskstatus)
        {
            OnTaskStatusCreated(taskstatus);

            var existingItem = Context.TaskStatuses
                              .Where(i => i.Id == taskstatus.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.TaskStatuses.Add(taskstatus);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(taskstatus).State = EntityState.Detached;
                throw;
            }

            OnAfterTaskStatusCreated(taskstatus);

            return taskstatus;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> CancelTaskStatusChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnTaskStatusUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);
        partial void OnAfterTaskStatusUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> UpdateTaskStatus(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus taskstatus)
        {
            OnTaskStatusUpdated(taskstatus);

            var itemToUpdate = Context.TaskStatuses
                              .Where(i => i.Id == taskstatus.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(taskstatus);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterTaskStatusUpdated(taskstatus);

            return taskstatus;
        }

        partial void OnTaskStatusDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);
        partial void OnAfterTaskStatusDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskStatus> DeleteTaskStatus(int id)
        {
            var itemToDelete = Context.TaskStatuses
                              .Where(i => i.Id == id)
                              .Include(i => i.Tasks)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnTaskStatusDeleted(itemToDelete);


            Context.TaskStatuses.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterTaskStatusDeleted(itemToDelete);

            return itemToDelete;
        }
    
        public async Task ExportTaskTypesToExcel(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/tasktypes/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/tasktypes/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        public async Task ExportTaskTypesToCSV(Query query = null, string fileName = null)
        {
            navigationManager.NavigateTo(query != null ? query.ToUrl($"export/radzencrm/tasktypes/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/radzencrm/tasktypes/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
        }

        partial void OnTaskTypesRead(ref IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> items);

        public async Task<IQueryable<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType>> GetTaskTypes(Query query = null)
        {
            var items = Context.TaskTypes.AsQueryable();


            if (query != null)
            {
                if (!string.IsNullOrEmpty(query.Expand))
                {
                    var propertiesToExpand = query.Expand.Split(',');
                    foreach(var p in propertiesToExpand)
                    {
                        items = items.Include(p.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(query.Filter))
                {
                    if (query.FilterParameters != null)
                    {
                        items = items.Where(query.Filter, query.FilterParameters);
                    }
                    else
                    {
                        items = items.Where(query.Filter);
                    }
                }

                if (!string.IsNullOrEmpty(query.OrderBy))
                {
                    items = items.OrderBy(query.OrderBy);
                }

                if (query.Skip.HasValue)
                {
                    items = items.Skip(query.Skip.Value);
                }

                if (query.Top.HasValue)
                {
                    items = items.Take(query.Top.Value);
                }
            }

            OnTaskTypesRead(ref items);

            return await Task.FromResult(items);
        }

        partial void OnTaskTypeGet(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> GetTaskTypeById(int id)
        {
            var items = Context.TaskTypes
                              .AsNoTracking()
                              .Where(i => i.Id == id);

  
            var itemToReturn = items.FirstOrDefault();

            OnTaskTypeGet(itemToReturn);

            return await Task.FromResult(itemToReturn);
        }

        partial void OnTaskTypeCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);
        partial void OnAfterTaskTypeCreated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> CreateTaskType(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType tasktype)
        {
            OnTaskTypeCreated(tasktype);

            var existingItem = Context.TaskTypes
                              .Where(i => i.Id == tasktype.Id)
                              .FirstOrDefault();

            if (existingItem != null)
            {
               throw new Exception("Item already available");
            }            

            try
            {
                Context.TaskTypes.Add(tasktype);
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(tasktype).State = EntityState.Detached;
                throw;
            }

            OnAfterTaskTypeCreated(tasktype);

            return tasktype;
        }

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> CancelTaskTypeChanges(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item)
        {
            var entityToCancel = Context.Entry(item);
            if (entityToCancel.State == EntityState.Modified)
            {
              entityToCancel.CurrentValues.SetValues(entityToCancel.OriginalValues);
              entityToCancel.State = EntityState.Unchanged;
            }

            return item;
        }

        partial void OnTaskTypeUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);
        partial void OnAfterTaskTypeUpdated(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> UpdateTaskType(int id, CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType tasktype)
        {
            OnTaskTypeUpdated(tasktype);

            var itemToUpdate = Context.TaskTypes
                              .Where(i => i.Id == tasktype.Id)
                              .FirstOrDefault();

            if (itemToUpdate == null)
            {
               throw new Exception("Item no longer available");
            }
                
            var entryToUpdate = Context.Entry(itemToUpdate);
            entryToUpdate.CurrentValues.SetValues(tasktype);
            entryToUpdate.State = EntityState.Modified;

            Context.SaveChanges();

            OnAfterTaskTypeUpdated(tasktype);

            return tasktype;
        }

        partial void OnTaskTypeDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);
        partial void OnAfterTaskTypeDeleted(CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType item);

        public async Task<CRMBlazorWasmRBS.Server.Models.RadzenCRM.TaskType> DeleteTaskType(int id)
        {
            var itemToDelete = Context.TaskTypes
                              .Where(i => i.Id == id)
                              .Include(i => i.Tasks)
                              .FirstOrDefault();

            if (itemToDelete == null)
            {
               throw new Exception("Item no longer available");
            }

            OnTaskTypeDeleted(itemToDelete);


            Context.TaskTypes.Remove(itemToDelete);

            try
            {
                Context.SaveChanges();
            }
            catch
            {
                Context.Entry(itemToDelete).State = EntityState.Unchanged;
                throw;
            }

            OnAfterTaskTypeDeleted(itemToDelete);

            return itemToDelete;
        }
        }
}