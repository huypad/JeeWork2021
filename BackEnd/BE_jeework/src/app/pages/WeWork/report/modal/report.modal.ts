export class ChartModal{
    // thuộc tính của biểu đồ
	label: string[] = [];
	datasets:  any[] = []; // asc || desc
  type:string = '';
  legend:boolean =false;
  options: any = {
      responsive: true
  };
  color:any[] = [];
  titleLegend:any[] = [];
  constructor(){
  }
}

// MuctieuDepartment = {
//   legend:false,
//   label:[],
//   datasets:[],
//   chartType:'pie',
//   color: [],
// }