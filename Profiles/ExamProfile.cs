using AutoMapper;
using ExaminationSystem.DTO.Exam;
using ExaminationSystem.Models;
using ExaminationSystem.ViewModels.Exam;

namespace ExaminationSystem.Profiles
{
    public class ExamProfile : Profile
    {
        public ExamProfile()
        {
            CreateMap<ExamViewModel, ExamDTO>().ReverseMap();
            CreateMap<ExamCreateViewModel, ExamCreateDTO>().ReverseMap();

            CreateMap<ExamDTO, ExaminationSystem.Models.Exam>().ReverseMap();
            CreateMap<ExamCreateDTO, ExaminationSystem.Models.Exam>().ReverseMap();

            CreateMap<ExamStudentCreateViewModel, ExamStudentCreateDTO>().ReverseMap();
            CreateMap<ExamStudentViewModel, ExamStudentDTO>().ReverseMap();

            CreateMap<ExamStudentCreateDTO, ExamStudent>();
            CreateMap<ExamStudentDTO, ExamStudent>().ReverseMap(); 
            CreateMap<ExamStudent, ExamStudentDTO>(); 
            CreateMap<ExamStudentCreateDTO, ExamStudent>();
            CreateMap<ExamStudent, ExamStudentCreateDTO>();
            CreateMap<ExamAnswerDTO, ExamAnswer>().ReverseMap();
        }
    }
}