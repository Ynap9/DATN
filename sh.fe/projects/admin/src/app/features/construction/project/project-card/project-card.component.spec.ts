import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { IViewProjectPair } from '@/models/project.model';
import { PROJECT_STATUS } from '@/shared/constants/project.constants';
import { ProjectCardComponent } from './project-card.component';

describe('ProjectCardComponent', () => {
    let fixture: ComponentFixture<ProjectCardComponent>;

    const project: IViewProjectPair = {
        projectId: 1,
        projectName: 'Số hoá văn bản 2026',
        status: PROJECT_STATUS.ACCEPTING,
        startedAt: '2026-11-01',
        endedAt: null,
        isQaEnabled: false,
        urlGoogleSheetSync: null,
        creatorName: 'admin02',
        documentTypeId: 10,
        documentTypeName: 'Văn bản hành chính',
        viewerRole: 'Admin',
        memberCount: 5,
        totalBox: 0,
        totalProfileRange: 0,
        totalSubmission: 1200,
        checkedCount: 74,
        qaPassedCount: null,
        progressPercent: 140,
        filledFieldCount: null,
    };

    function render(): HTMLElement {
        fixture.componentRef.setInput('project', project);
        fixture.componentRef.setInput('actions', [{ type: 'view', label: 'Xem chi tiết', icon: 'pi pi-eye' }]);
        fixture.detectChanges();
        return fixture.nativeElement;
    }

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideNoopAnimations(), providePrimeNG()] });
        fixture = TestBed.createComponent(ProjectCardComponent);
    });

    it('hiện tên, số liệu thật, QA null là — và tiến độ chặn 100%', () => {
        const element = render();
        const stats = Array.from(element.querySelectorAll('.project-card-stats dd')).map((cell) => cell.textContent!.trim());

        expect(element.querySelector('h3')!.textContent!.trim()).toBe('Số hoá văn bản 2026');
        expect(stats).toEqual([new Intl.NumberFormat('vi-VN').format(1200), '74', '—', '5']);
        expect(element.querySelector('.project-card-progress-label strong')!.textContent!.trim()).toBe('100%');
        expect(element.querySelector('.status-badge--amber')!.textContent!.trim()).toBe('Đang nghiệm thu');
    });

    it('khoảng ngày: cùng năm bỏ năm ngày đầu, thiếu cả hai ngày hiện —', () => {
        fixture.componentRef.setInput('project', { ...project, startedAt: '2026-11-01', endedAt: '2026-12-30' });
        fixture.componentRef.setInput('actions', []);
        expect(fixture.componentInstance.period()).toBe('01/11 – 30/12/2026');

        fixture.componentRef.setInput('project', { ...project, startedAt: '2025-11-01', endedAt: null });
        expect(fixture.componentInstance.period()).toBe('01/11/2025 – —');

        fixture.componentRef.setInput('project', { ...project, startedAt: null, endedAt: null });
        expect(fixture.componentInstance.period()).toBe('—');
    });

    it('bấm thẻ mở chi tiết, bấm ⋮ thì không', () => {
        const element = render();
        const opened = jasmine.createSpy('open');
        fixture.componentInstance.open.subscribe(opened);

        (element.querySelector('app-tbl-action button') as HTMLButtonElement).click();
        expect(opened).not.toHaveBeenCalled();

        (element.querySelector('.project-card') as HTMLElement).click();
        expect(opened).toHaveBeenCalledTimes(1);
    });
});
