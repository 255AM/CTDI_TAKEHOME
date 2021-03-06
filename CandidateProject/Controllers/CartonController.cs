using CandidateProject.EntityModels;
using CandidateProject.ViewModels;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace CandidateProject.Controllers
{
    public class CartonController : Controller
    {
        private CartonContext db = new CartonContext();

        // GET: Carton
        public ActionResult Index()
        {
        // added a display of number of items in cart to index screen

            var cartons = db.Cartons
                .Include(c => c.CartonDetails)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber,
                    NoOfItems = db.CartonDetails.Where(d => d.CartonId == c.Id).ToList().Count
                })
                .ToList();
        return View(cartons);
        }

        // GET: Carton/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // GET: Carton/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carton/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CartonNumber")] Carton carton)
        {
            if (ModelState.IsValid)
            {
                db.Cartons.Add(carton);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(carton);
        }

        // GET: Carton/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CartonNumber")] CartonViewModel cartonViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons.Find(cartonViewModel.Id);
                carton.CartonNumber = cartonViewModel.CartonNumber;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cartonViewModel);
        }

        // GET: Carton/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var context = new CartonContext())
            {
                //removing mentions of parent to allow deletion of carton with items

                var parent = context.Cartons.Include(p => p.CartonDetails)
                    .SingleOrDefault(p => p.Id == id);

                foreach (var child in parent.CartonDetails.ToList())
                    context.CartonDetails.Remove(child);
                    context.SaveChanges();
            }
            Carton carton = db.Cartons.Find(id);
            db.Cartons.Remove(carton);
            db.SaveChanges();


            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult AddEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
                
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id
                })
                .SingleOrDefault();

            if (carton == null)
            {
                return HttpNotFound();
            }

            //added code to dissallow the addition of one item to multiple cartons

            var equipment = db.Equipments
                    .Where(e => !db.CartonDetails
                        .Select(cd => cd.EquipmentId)
                        .Contains(e.Id))
                         .Select(e => new EquipmentViewModel()
                         {
                             Id = e.Id,
                             ModelType = e.ModelType.TypeName,
                             SerialNumber = e.SerialNumber
                         })
                    .ToList();
                carton.Equipment = equipment;
                return View(carton);
        }

        public ActionResult AddEquipmentToCarton([Bind(Include = "CartonId,EquipmentId,NoOfItems")] AddEquipmentViewModel addEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons
                    .Include(c => c.CartonDetails)
                    .Where(c => c.Id == addEquipmentViewModel.CartonId)
                    .SingleOrDefault();
                if (carton == null)
                {
                    return HttpNotFound();
                }
                var equipment = db.Equipments
                    .Where(e => e.Id == addEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();
                if (equipment == null)
                {
                    return HttpNotFound();
                }
                var detail = new CartonDetail()
                {
                    Carton = carton,
                    Equipment = equipment
                };

                //added logic to prevent adding > 10 items to a carton. User is alerted when attempting to do so

                var ItemCount = db.CartonDetails.Where(p => p.CartonId == addEquipmentViewModel.CartonId).ToList().Count;
                if (ItemCount < 10)
                {
                    carton.CartonDetails.Add(detail);
                    db.SaveChanges();
                    TempData["Warning"] = "";
                }

                else
                {
                    TempData["Warning"] = "Container Is Full. Item Not Added";
                }
                
            }
            return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
        }

        public ActionResult ViewCartonEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id,
                    Equipment = c.CartonDetails
                        .Select(cd => new EquipmentViewModel()
                        {
                            Id = cd.EquipmentId,
                            ModelType = cd.Equipment.ModelType.TypeName,
                            SerialNumber = cd.Equipment.SerialNumber
                        })
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        public ActionResult RemoveEquipmentOnCarton([Bind(Include = "CartonId,EquipmentId")] RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            //added remove functionality to allow the removal of an item from the container;
            using (var context = new CartonContext())

                if (ModelState.IsValid)
                {
                    var itemToRemove = context.CartonDetails.SingleOrDefault(x => x.CartonId == removeEquipmentViewModel.CartonId && x.EquipmentId == removeEquipmentViewModel.EquipmentId);
                    context.CartonDetails.Remove(itemToRemove);
                    context.SaveChanges();
                }
            return RedirectToAction("ViewCartonEquipment", new { id = removeEquipmentViewModel.CartonId });
        }

        public ActionResult RemoveAllEquipmentOnCarton([Bind(Include = "CartonId")] RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            //added a quick removal of all items in cart

            using (var context = new CartonContext())

                if (ModelState.IsValid)
                {
                    var parent = context.Cartons.Include(p => p.CartonDetails)
                        .SingleOrDefault(p => p.Id == removeEquipmentViewModel.CartonId);

                    foreach (var child in parent.CartonDetails.ToList())
                        context.CartonDetails.Remove(child);
                        context.SaveChanges();
                }

                return RedirectToAction("ViewCartonEquipment", new { id = removeEquipmentViewModel.CartonId });
        }

    }
}
