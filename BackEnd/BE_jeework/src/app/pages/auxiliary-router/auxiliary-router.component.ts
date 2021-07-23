import { ProjectsTeamService } from './../WeWork/projects-team/Services/department-and-project.service';
import { MatDialog } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';
import { Component, OnInit } from '@angular/core';
import { WorkListNewDetailComponent } from '../WeWork/projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';

@Component({
  selector: 'app-auxiliary-router',
  templateUrl: './auxiliary-router.component.html',
  styleUrls: ['./auxiliary-router.component.scss']
})
export class AuxiliaryRouterComponent implements OnInit {

  constructor(
    private router: Router,
    public dialog: MatDialog,
    public ProjectsTeamService: ProjectsTeamService,
    private activatedRoute: ActivatedRoute
  ) { }

  ngOnInit() {
    this.activatedRoute.params.subscribe(params => {
			var ID = params.id;
      console.log(params,'data router')
      this.LoadDetailTask(ID);
      // setTimeout(() => {
      //   this.close();
      // }, 1000);
		});
  }
  LoadDetailTask(id){
    this.ProjectsTeamService.WorkDetail(id).subscribe(res => {
      if(res && res.status==1){
        this.openDialog(res.data);
      }else{
        alert(res.error.message);
      }
    })
  }

  close() {
    // this.router.navigate([{ outlets: { aux: null } }])
    console.log('123')
    this.router.navigate(['', {outlets: {auxName: null}}]);
  }

  openDialog(item){
    item.notback = true;
    const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
      width: '100vw',
      height: '100vh',
      data: item
    });

    dialogRef.afterClosed().subscribe(result => {
      this.close();
    });
    // const dialogRef = this.dialog.open(WorkListNewDetailComponent, { 
    //   width: "40vw",
    //   minHeight: "60vh",
    //   data: {ID} });
		// dialogRef.afterClosed().subscribe(res => {
		// 	setTimeout(() => {
    //     console.log('out');
    //     this.close();
    //   }, 50);
		// });
  }
}