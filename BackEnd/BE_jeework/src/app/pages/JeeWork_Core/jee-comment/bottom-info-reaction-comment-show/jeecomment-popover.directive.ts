import { Overlay, OverlayPositionBuilder, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { ComponentRef, Directive, ElementRef, HostListener, Input, OnDestroy, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { Subject } from 'rxjs';
import { BottomInfoReactionCommentShowComponent } from './bottom-info-reaction-comment-show.component';

@Directive({ selector: '[jeecommentPopover]' })
export class JeecommentPopoverDirective implements OnInit {
  @Input('jeecommentPopover')
  popoverTrigger: string = '';

  private overlayRef: OverlayRef;

  constructor(private overlay: Overlay, private overlayPositionBuilder: OverlayPositionBuilder, private elementRef: ElementRef) {}

  ngOnInit(): void {
    const positionStrategy = this.overlayPositionBuilder.flexibleConnectedTo(this.elementRef).withPositions([
      {
        originX: 'center',
        originY: 'top',
        overlayX: 'center',
        overlayY: 'bottom',
        offsetY: -8,
      },
    ]);

    this.overlayRef = this.overlay.create({ positionStrategy });
  }

  @HostListener('mouseenter')
  show() {
    const tooltipRef: ComponentRef<BottomInfoReactionCommentShowComponent> = this.overlayRef.attach(
      new ComponentPortal(BottomInfoReactionCommentShowComponent)
    );
    tooltipRef.instance.text = this.popoverTrigger;
  }

  @HostListener('mouseout')
  hide() {
    this.overlayRef.detach();
  }
}
