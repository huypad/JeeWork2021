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

// public class BaoCaoThongKe
// {
//     public string Ten { get; set; }
//     public string col1 { get; set; }
//     public string col2 { get; set; }
//     public string col3 { get; set; }
//     public string col4 { get; set; }
//     public string col5 { get; set; }
// }
export class BaoCaoThongKeModel {
  Ten: string;
  col1: string;
  col2: string;
  col3: string;
  col4: string;
  col5: string;
  constructor(
    Ten: string = '0',
		col1: string = '0',
		col2: string = '0',
		col3: string = '0',
		col4: string = '0',
		col5: string = '0') {
		this.Ten = Ten;
		this.col1 = col1;
		this.col2 = col2;
		this.col3 = col3;
		this.col4 = col4;
		this.col5 = col5;
	}
}
export class MemberProjectModel {
  Ten: string;
  col1: string;
  col2: string;
  col3: string;
  col4: string;
  col5: string;
  col6: string;
  col7: string;
  constructor(
    Ten: string = '0',
		col1: string = '0',
		col2: string = '0',
		col3: string = '0',
		col4: string = '0',
		col5: string = '0',
		col6: string = '0',
		col7: string = '0',
    ) 
    {
		this.Ten = Ten;
		this.col1 = col1;
		this.col2 = col2;
		this.col3 = col3;
		this.col4 = col4;
		this.col5 = col5;
		this.col6 = col6;
		this.col7 = col7;
	}
}