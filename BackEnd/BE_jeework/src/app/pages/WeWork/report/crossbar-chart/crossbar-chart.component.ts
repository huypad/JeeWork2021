import { Component, OnInit,Input } from '@angular/core';

@Component({
  selector: 'kt-crossbar-chart',
  templateUrl: './crossbar-chart.component.html',
})
export class CrossbarChartComponent implements OnInit {

  constructor() { }

  @Input() data: any = undefined;
  @Input() color: any = undefined;
  @Input() title: any = [];

  ngOnInit() {
    if(this.color==undefined)(
      this.color = ['red','blue','yellow','green','violet']
    )
    this.Xulydata();
  }
  chart :any = [];
  Xulydata(){
    var sum = this.data.reduce((a, b) => a + b);
    if(sum > 0){
      for(let i = 0;i<this.data.length; i++){
        this.chart.push({
          'width': (this.data[i]/sum)*100 + '%',
          'color':this.color[i],
          title:this.title[i],
        });
      }
    }else{
      this.chart = null;
    }
  }


}
